using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using Helper = Neo.SmartContract.Framework.Helper;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Nep5Contract
{
    public class ContractNep55Gas : SmartContract
    {
        public delegate void deleTransfer(byte[] from, byte[] to, BigInteger value);
        [DisplayName("transfer")]
        public static event deleTransfer Transferred;

        public delegate void deleRefundTarget(byte[] txid, byte[] who);
        [DisplayName("onRefundTarget")]
        public static event deleRefundTarget OnRefundTarget;

        private static readonly byte[] AssetId = Helper.HexToBytes("e72d286979ee6cb1b7e65dfddfb2e384100b8d148e7758de42e4168b71792c60"); //全局资产的资产ID，逆序，这里是NeoGas

        private static readonly byte[] superAdmin = Helper.ToScriptHash("Ad1HKAATNmFT5buNgSxspbW68f4XVSssSw");//管理员

        private static readonly string Name = "NEP5 GAS";
        private static readonly string Symbol = "SGAS";
        private static readonly byte Decimals = 8;

        public static object Main(string method, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                var tx = ExecutionEngine.ScriptContainer as Transaction;
                var currentHash = ExecutionEngine.ExecutingScriptHash;
                var inputs = tx.GetInputs();
                var outputs = tx.GetOutputs();

                //检查输入是不是有被标记过
                for (var i = 0; i < inputs.Length; i++)
                {
                    if (inputs[i].PrevIndex == 0)//如果 utxo n 为0 的话，是有可能是一个标记utxo的
                    {
                        byte[] refundMan = Storage.Get(Storage.CurrentContext, inputs[i].PrevHash); //0.1
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
                if (method == "totalSupply") return Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger(); //0.1

                if (method == "name") return Name;

                if (method == "symbol") return Symbol;

                if (method == "decimals") return Decimals;

                if (method == "balanceOf") return Storage.Get(Storage.CurrentContext, (byte[])args[0]).AsBigInteger(); //0.1

                if (method == "transfer") return Transfer((byte[])args[0], (byte[])args[1], (BigInteger)args[2]);

                if (method == "mintTokens") return MintTokens();

                if (method == "refund") return Refund((byte[])args[0]);

                if (method == "getRefundTarget") return Storage.Get(Storage.CurrentContext, (byte[])args[0]);

                if (method == "upgrade")
                {
                    if (args.Length != 1 && args.Length != 9)
                        return false;
                    byte[] currentScript = Blockchain.GetContract(ExecutionEngine.ExecutingScriptHash).Script; //0.1
                    byte[] newScript = (byte[])args[0];
                    if (newScript == currentScript)
                        return false;
                    if (!Runtime.CheckWitness(superAdmin)) //0.2
                        return false;
                    Contract.Migrate( //500
                        script: (byte[])args[0],
                        parameter_list: (byte[])args[1],
                        return_type: (byte)args[2],
                        need_storage: (bool)args[3],
                        name: (string)args[4],
                        version: (string)args[5],
                        author: (string)args[6],
                        email: (string)args[7],
                        description: (string)args[8]);
                    return true;
                }
            }
            return false;
        }

        public static bool Transfer(byte[] from, byte[] to, BigInteger amount)
        {
            //形参校验
            if (from == to)
                return true;
            if (from.Length != 20 || to.Length != 20 || amount <= 0 || !IsPayable(to) || !Runtime.CheckWitness(from)/*0.2*/)
                return false;

            //付款人减少余额
            var fromAmount = Storage.Get(Storage.CurrentContext, from).AsBigInteger(); //0.1
            if (fromAmount < amount)
                return false;
            else if (fromAmount == amount)
                Storage.Delete(Storage.CurrentContext, from); //0.1
            else
                Storage.Put(Storage.CurrentContext, from, fromAmount - amount); //1

            //收款人增加余额
            var toAmount = Storage.Get(Storage.CurrentContext, to).AsBigInteger(); //0.1
            Storage.Put(Storage.CurrentContext, to, toAmount + amount); //1

            //通知
            Transferred(from, to, amount);
            return true;
        }
        
        /// <summary>
        /// 全局资产 -> NEP5资产
        /// </summary>
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
            var totalSupply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger(); //0.1
            totalSupply += value;
            Storage.Put(Storage.CurrentContext, "totalSupply", totalSupply); //1

            //分发资产
            var amount = Storage.Get(Storage.CurrentContext, sender).AsBigInteger(); //0.1
            Storage.Put(Storage.CurrentContext, sender, amount + value); //1

            //通知
            Transferred(null, sender, value);

            return true;
        }

        /// <summary>
        /// NEP5资产 -> 全局资产
        /// 用户在发起 Refund 时需要构造一个从合约地址到合约地址的转账，转账金额等于用户想退回的金额（如有找零也要找零到合约地址），然后智能合约会对其进行标记。
        /// </summary>
        public static bool Refund(byte[] from)
        {
            var tx = ExecutionEngine.ScriptContainer as Transaction;
            //0 号 output 是用户待退回的资产
            var preRefund = tx.GetOutputs()[0];
            //退回的资产不对，退回失败
            if (preRefund.AssetId.AsBigInteger() != AssetId.AsBigInteger())
                return false;
            //不是转给自身，退回失败
            if (preRefund.ScriptHash.AsBigInteger() != ExecutionEngine.ExecutingScriptHash.AsBigInteger())
                return false;
            //因为 Refund 的交易的 inputs 和 outputs 都来自合约地址，所以很可能多个人构造相同的交易。
            //如果当前的交易已经被其它人标记为待退回，则退回失败
            if (Storage.Get(Storage.CurrentContext, tx.Hash).Length > 0) //0.1
                return false;
            //不是本人申请的，退回失败
            if (!Runtime.CheckWitness(from)) //0.2
                return false;

            //付款人减少余额
            var fromAmount = Storage.Get(Storage.CurrentContext, from).AsBigInteger(); //0.1
            var preRefundValue = preRefund.Value;
            if (fromAmount < preRefundValue)
                return false;
            else if (fromAmount == preRefundValue)
                Storage.Delete(Storage.CurrentContext, from); //0.1
            else
                Storage.Put(Storage.CurrentContext, from, fromAmount - preRefundValue); //1

            //对待退回的 output 进行标记（实际只标记 txid，output index 默认为 0）
            Storage.Put(Storage.CurrentContext, tx.Hash, from); //1

            //改变总量
            var totalSupply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger(); //0.1
            totalSupply -= preRefundValue;
            Storage.Put(Storage.CurrentContext, "totalSupply", totalSupply); //1

            //通知
            OnRefundTarget(tx.Hash, from);

            return true;
        }        

        public static bool IsPayable(byte[] to)
        {
            var c = Blockchain.GetContract(to); //0.1
            return c == null || c.IsPayable;
        }
    }
}