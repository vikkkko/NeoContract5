using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using Helper = Neo.SmartContract.Framework.Helper;
using System.ComponentModel;
using System.Numerics;
using System;

namespace SGAS
{
    public class SGAS : SmartContract
    {
        [DisplayName("transfer")]
        public static event deleTransfer Transferred;
        public delegate void deleTransfer(byte[] from, byte[] to, BigInteger value);

        [DisplayName("refund")]
        public static event deleRefundTarget Refunded;
        public delegate void deleRefundTarget(byte[] txid, byte[] who);

        private static readonly byte[] AssetId = Helper.HexToBytes("e72d286979ee6cb1b7e65dfddfb2e384100b8d148e7758de42e4168b71792c60"); //全局资产的资产ID，逆序，这里是NeoGas

        //StorageMap: contract, refund, asset, txInfo,
        private static StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract)); //key: "totalSupply", "lastTx"
        private static StorageMap refund = Storage.CurrentContext.CreateMap(nameof(refund)); //key: txHash
        private static StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset)); //key: account
        private static StorageMap txInfo = Storage.CurrentContext.CreateMap(nameof(txInfo)); //key: txHash

        public static object Main(string method, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                var tx = ExecutionEngine.ScriptContainer as Transaction;
                var inputs = tx.GetInputs();
                var outputs = tx.GetOutputs();

                //检查输入是不是有被标记过
                
                for (var i = 0; i < inputs.Length; i++)
                {
                    if (inputs[i].PrevIndex == 0)//如果 utxo n 为0 的话，是有可能是一个标记utxo的
                    {
                        var refundMan = refund.Get(inputs[i].PrevHash); //0.1
                        //检测到标记为待退回的 input
                        if (refundMan.Length > 0)
                        {
                            //退回时只允许一个 input，一个 output
                            if (inputs.Length != 1 || outputs.Length != 1)
                                return false;
                            //如果只有一个输入，一个输出，并且目的转账地址就是授权地址，允许转账
                            return outputs[0].ScriptHash.AsBigInteger() == refundMan.AsBigInteger();
                        }
                    }
                }
                var currentHash = ExecutionEngine.ExecutingScriptHash;
                //如果所有的 inputs 都没有被标记为待退回
                BigInteger inputAmount = 0;
                foreach (var refe in tx.GetReferences())
                {
                    if (refe.AssetId.AsBigInteger() != AssetId.AsBigInteger())
                        return false;//不允许操作除gas以外的

                    if (refe.ScriptHash.AsBigInteger() != currentHash.AsBigInteger())
                        return false;//不允许混入其它地址

                    inputAmount += refe.Value;
                }
                //检查有没有钱离开本合约
                BigInteger outputAmount = 0;
                foreach (var output in outputs)
                {
                    if (output.ScriptHash.AsBigInteger() != currentHash.AsBigInteger())
                        return false;
                    outputAmount += output.Value;
                }
                return outputAmount == inputAmount;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                var callscript = ExecutionEngine.CallingScriptHash;

                if (ExecutionEngine.EntryScriptHash.AsBigInteger() != callscript.AsBigInteger()) return false;

                if (method == "balanceOf") return BalanceOf((byte[])args[0]);

                if (method == "decimals") return Decimals();

                if (method == "getRefundTarget") return GetRefundTarget((byte[])args[0]);

                if (method == "getTxInfo") return GetTxInfo((byte[])args[0]);

                if (method == "mintTokens") return MintTokens();

                if (method == "name") return Name();

                if (method == "refund") return Refund((byte[])args[0]);

                if (method == "symbol") return Symbol();
                
                if (method == "supportedStandards") return SupportedStandards();

                if (method == "totalSupply") return TotalSupply();

                if (method == "transfer") return Transfer((byte[])args[0], (byte[])args[1], (BigInteger)args[2]);

                if (method == "transferAPP") return TransferAPP((byte[])args[0], (byte[])args[1], (BigInteger)args[2], callscript);
            }
            //else if (Runtime.Trigger == TriggerType.VerificationR) //向后兼容
            //{
            //    if (method != "mintTokens") return false;
            //    var tx = ExecutionEngine.ScriptContainer as Transaction;
            //    foreach (var output in tx.GetOutputs())
            //    {
            //        if (output.ScriptHash == currentHash && output.AssetId.AsBigInteger() != AssetId.AsBigInteger())
            //            return false;
            //    }
            //    return true;
            //}
            return false;
        }

        [DisplayName("balanceOf")]
        public static BigInteger BalanceOf(byte[] account)
        {
            if (account.Length != 20)
                throw new InvalidOperationException("The parameters account SHOULD be 20-byte addresses.");
            return asset.Get(account).AsBigInteger(); //0.1
        }
        [DisplayName("decimals")]
        public static byte Decimals() => 8;

        [DisplayName("getRefundTarget")]
        public static byte[] GetRefundTarget(byte[] txid)
        {
            if (txid.Length != 32)
                throw new InvalidOperationException("The parameters txid SHOULD be 32-byte tx hash.");
            return refund.Get(txid); //0.1
        }

        [DisplayName("getTxInfo")]
        public static TransferInfo GetTxInfo(byte[] txid)
        {
            if (txid.Length != 32)
                throw new InvalidOperationException("The parameters txid SHOULD be 32-byte tx hash.");
            var result = txInfo.Get(txid); //0.1
            if (result.Length == 0) return null;
            return Helper.Deserialize(result) as TransferInfo;
        }

        private static bool IsPayable(byte[] to)
        {
            var c = Blockchain.GetContract(to); //0.1
            return c == null || c.IsPayable;
        }

        /// <summary>
        /// 全局资产 -> NEP5资产
        /// </summary>
        [DisplayName("mintTokens")]
        public static bool MintTokens()
        {
            var tx = ExecutionEngine.ScriptContainer as Transaction;

            //发送全局资产的人，接收NEP5资产的人
            byte[] sender = null;
            var inputs = tx.GetReferences();
            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i].AssetId.AsBigInteger() == AssetId.AsBigInteger())
                {
                    sender = inputs[i].ScriptHash;
                    break;
                }
            }
            
            var lastTx = contract.Get("lasttx"); //0.1
            if (tx.Hash == lastTx) return false;
            contract.Put("lasttx", tx.Hash); //1

            if (sender.AsBigInteger() == ExecutionEngine.ExecutingScriptHash.AsBigInteger()) return false;

            //兑换数量
            var outputs = tx.GetOutputs();
            ulong value = 0;
            foreach (var output in outputs)
            {
                if (output.ScriptHash == ExecutionEngine.ExecutingScriptHash &&
                    output.AssetId.AsBigInteger() == AssetId.AsBigInteger())
                {
                    value += (ulong)output.Value;
                }
            }

            //增加合约资产的总量
            var totalSupply = contract.Get("totalSupply").AsBigInteger(); //0.1
            totalSupply += value;
            contract.Put("totalSupply", totalSupply); //1

            //分发资产
            var amount = asset.Get(sender).AsBigInteger(); //0.1
            asset.Put(sender, amount + value); //1

            //通知
            SetTxInfo(null, sender, value);
            Transferred(null, sender, value);
            return true;
        }

        [DisplayName("name")]
        public static string Name() => "NEP5 GAS";

        /// <summary>
        /// NEP5资产 -> 全局资产
        /// 用户在发起 Refund 时需要构造一个从合约地址到合约地址的转账，转账金额等于用户想退回的金额（如有找零也要找零到合约地址），然后智能合约会对其进行标记。
        /// </summary>
        [DisplayName("refund")]
        public static bool Refund(byte[] from)
        {
            if (from.Length != 20)
                throw new InvalidOperationException("The parameters from SHOULD be 20-byte addresses.");
            var tx = ExecutionEngine.ScriptContainer as Transaction;
            //0 号 output 是用户待退回的资产
            var preRefund = tx.GetOutputs()[0];
            //退回的资产不对，退回失败
            if (preRefund.AssetId.AsBigInteger() != AssetId.AsBigInteger()) return false;

            //不是转给自身，退回失败
            if (preRefund.ScriptHash.AsBigInteger() != ExecutionEngine.ExecutingScriptHash.AsBigInteger()) return false;
            
            //因为 Refund 的交易的 inputs 和 outputs 都来自合约地址，所以很可能多个人构造相同的交易。如果当前的交易已经被其它人标记为待退回，则退回失败
            if (refund.Get(tx.Hash).Length > 0) return false; //0.1

            //不是本人申请的，退回失败
            if (!Runtime.CheckWitness(from)) return false; //0.2

            //付款人减少余额
            var fromAmount = asset.Get(from).AsBigInteger(); //0.1
            var preRefundValue = preRefund.Value;
            if (fromAmount < preRefundValue)
                return false;
            else if (fromAmount == preRefundValue)
                asset.Delete(from); //0.1
            else
                asset.Put(from, fromAmount - preRefundValue); //1

            //对待退回的 output 进行标记（实际只标记 txid，output index 默认为 0）
            refund.Put(tx.Hash, from); //1

            //改变总量
            var totalSupply = contract.Get("totalSupply").AsBigInteger(); //0.1
            totalSupply -= preRefundValue;
            contract.Put("totalSupply", totalSupply); //1

            //通知
            Refunded(tx.Hash, from);
            return true;
        }
        
        private static void SetTxInfo(byte[] from, byte[] to, BigInteger value)
        {
            var txid = (ExecutionEngine.ScriptContainer as Transaction).Hash;
            TransferInfo info = new TransferInfo
            {
                from = from,
                to = to,
                value = value
            };
            txInfo.Put(txid, Helper.Serialize(info)); //1
        }

        [DisplayName("symbol")]
        public static string Symbol() => "SGAS";

        [DisplayName("supportedStandards")]
        public static object SupportedStandards() => "{\"NEP-5\", \"NEP-7\", \"NEP-10\"}";

        [DisplayName("totalSupply")]
        public static BigInteger TotalSupply() => contract.Get("totalSupply").AsBigInteger(); //0.1

        [DisplayName("transfer")]
        public static bool Transfer(byte[] from, byte[] to, BigInteger amount)
        {
            //形参校验
            if (from.Length != 20 || to.Length != 20)
                throw new InvalidOperationException("The parameters from and to SHOULD be 20-byte addresses.");
            if (amount <= 0)
                throw new InvalidOperationException("The parameter amount MUST be greater than or equal to 0.");
            if (!IsPayable(to) || !Runtime.CheckWitness(from)/*0.2*/)
                return false;
            var fromAmount = asset.Get(from).AsBigInteger(); //0.1
            if (fromAmount < amount)
                return false;
            if (from == to)
                return true;

            //付款人减少余额
            if (fromAmount == amount)
                Storage.Delete(Storage.CurrentContext, from); //0.1
            else
                asset.Put(from, fromAmount - amount); //1

            //收款人增加余额
            var toAmount = asset.Get(to).AsBigInteger(); //0.1
            asset.Put(to, toAmount + amount); //1

            //通知
            SetTxInfo(from, to, amount);
            Transferred(from, to, amount);
            return true;
        }

        [DisplayName("transferAPP")] //Only for ABI file
        public static bool TransferAPP(byte[] from, byte[] to, BigInteger amount)
        {
            return true;
        }

        //Methods of actual execution
        private static bool TransferAPP(byte[] from, byte[] to, BigInteger amount, byte[] callscript)
        {
            //形参校验
            if (from.Length != 20 || to.Length != 20)
                throw new InvalidOperationException("The parameters from and to SHOULD be 20-byte addresses.");
            if (amount <= 0)
                throw new InvalidOperationException("The parameter amount MUST be greater than or equal to 0.");
            if (!IsPayable(to) || from.AsBigInteger() != callscript.AsBigInteger())
                return false;
            var fromAmount = asset.Get(from).AsBigInteger(); //0.1
            if (fromAmount < amount)
                return false;
            if (from == to)
                return true;

            //付款人减少余额
            if (fromAmount == amount)
                Storage.Delete(Storage.CurrentContext, from); //0.1
            else
                asset.Put(from, fromAmount - amount); //1

            //收款人增加余额
            var toAmount = asset.Get(to).AsBigInteger(); //0.1
            asset.Put(to, toAmount + amount); //1

            //通知
            SetTxInfo(from, to, amount);
            Transferred(from, to, amount);
            return true;
        }
    }
}