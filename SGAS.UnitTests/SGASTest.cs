using System;
using System.Collections.Generic;
using System.Linq;
using Neo;
using Neo.Core;
using Neo.IO;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using Newtonsoft.Json.Linq;
using SGAS.UnitTests;

namespace NeoContract.UnitTests
{
    public class SGASTest
    {
        #region Variables

        public readonly UInt256 AssetId;

        public readonly UInt160 SGAS_ContractHash;
        public readonly byte[] SGAS_Contract;
        public readonly RpcClient RPC;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rpc">RPC</param>
        /// <param name="contract">Contract</param>
        /// <param name="token">Token</param>
        public SGASTest(RpcClient rpc, UInt160 contract, UInt256 token)
        {
            AssetId = token;
            SGAS_ContractHash = contract;

            var json = rpc.Call("getcontractstate", $"[\"{SGAS_ContractHash.ToString()}\"]");
            SGAS_Contract = json["result"]["script"].Value<string>().HexToBytes();

            RPC = rpc;
        }

        /// <summary>
        /// Sign transaction with wallet
        /// </summary>
        /// <param name="wallet">Wallet</param>
        /// <param name="tx">Transaction</param>
        /// <returns>Return signed transaction</returns>
        public Transaction SignTx(Wallet wallet, Transaction tx)
        {
            // Sign in wallet

            Console.ForegroundColor = ConsoleColor.Yellow;

            var context = new ContractParametersContext(tx);
            wallet.Sign(context);

            if (context.Completed)
            {
                Console.WriteLine("  > Sign Successful");
                tx.Scripts = context.GetScripts();
            }
            else
            {
                Console.WriteLine("  > Sign Faild");
            }

            DumpValues(tx);

            return tx;
        }

        /// <summary>
        /// Verify transaction with wallet
        /// </summary>
        /// <param name="tx">Transaction</param>
        /// <returns>Return signed transaction</returns>
        public void DumpValues(Transaction tx)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            try
            {
                tx = Transaction.DeserializeFrom(tx.ToArray());
            }
            catch
            {
                Console.WriteLine("  > Invalid Transaction Format");
            }

            Console.WriteLine("  > Hash: " + tx.Hash.ToString());
            Console.WriteLine("  > Verify Transaction: " + (tx is ContractTransaction ? "[skipped]" : tx.Verify(new List<Transaction> { tx }).ToString()));
            Console.WriteLine("  > Raw Transaction: " + tx.ToArray().ToHexString());

            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// SGAS MintTokens
        /// </summary>
        /// <param name="wallet">Wallet</param>
        /// <param name="sendValue">Send amount</param>
        /// <param name="inputTXHash">Input hash (there is no rpc call for this)</param>
        /// <returns></returns>
        public Transaction MintTokens(Wallet wallet, Fixed8 sendValue, UInt256 inputTXHash)
        {
            // -------------------------------------
            // Get values
            // -------------------------------------

            var from = wallet.GetAccounts().FirstOrDefault();
            var inputTx = RPC.GetTransaction(inputTXHash);
            var outputTxIndex = ushort.MaxValue;

            if (inputTx != null)
            {
                // Search for the index

                for (ushort x = 0; x < inputTx.Outputs.Length; x++)
                {
                    if (inputTx.Outputs[x].ScriptHash.ToString() == from.ScriptHash.ToString())
                    {
                        outputTxIndex = x;
                        break;
                    }
                }
            }

            if (outputTxIndex == ushort.MaxValue) throw new Exception("TX Output invalid");

            var originalOutput = inputTx.Outputs[outputTxIndex];

            // -------------------------------------

            var inputs = new CoinReference[]
            {
                // coin reference A

                new CoinReference()
                {
                    PrevHash = inputTx.Hash,
                    PrevIndex = outputTxIndex // X GAS
                }
            };

            var outputs = new TransactionOutput[]{ new TransactionOutput()
            {
                AssetId = AssetId, // Asset Id, this is GAS
                ScriptHash = SGAS_ContractHash, // SGAS
                Value = sendValue // sendValue
            },
            new TransactionOutput()
            {
                AssetId = AssetId, // Asset Id, this is GAS
                ScriptHash = originalOutput.ScriptHash, // Contract hash
                Value = originalOutput.Value-sendValue // X - sendValue [GAS]
            }};

            Transaction tx;

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(SGAS_ContractHash, "mintTokens");
                sb.Emit(OpCode.THROWIFNOT);

                // Should change the hash

                //byte[] nonce = new byte[8];
                //Random rand = new Random();
                //rand.NextBytes(nonce);
                //sb.Emit(OpCode.RET, nonce);

                tx = new InvocationTransaction
                {
                    Version = 1,
                    Script = sb.ToArray(),
                    Outputs = outputs,
                    Inputs = inputs,
                    Attributes = new TransactionAttribute[0],
                    Scripts = new Witness[0]
                };
            }

            // Sign tx

            return SignTx(wallet, tx);
        }

        /// <summary>
        /// Refund
        /// </summary>
        /// <param name="wallet">Wallet</param>
        /// <param name="txHashVerify">Tx verify</param>
        /// <param name="sendValue">Send value</param>
        /// <returns>Transaction</returns>
        public Transaction Refund(Wallet wallet, UInt256 txHashVerify, Fixed8 sendValue, UInt256 inputTXHash)
        {
            // -------------------------------------
            // Values
            // -------------------------------------

            var from = wallet.GetAccounts().FirstOrDefault().ScriptHash;

            // -------------------------------------

            var inputs = new CoinReference[]
            {
                new CoinReference()
                {
                    PrevHash = inputTXHash,
                    PrevIndex = 0 // Only one for simplify
                }
            };

            var outputs = new TransactionOutput[]
            {
                new TransactionOutput()
                {
                    AssetId = AssetId, //Asset Id, this is GAS
                    ScriptHash = from, //SGAS 地址
                    Value = sendValue //Value
                }
            };

            var applicationScript = new byte[0];

            using (var sb = new ScriptBuilder())
            {
                sb.EmitAppCall(SGAS_ContractHash, "refund", from);
                sb.Emit(OpCode.THROWIFNOT);
                applicationScript = sb.ToArray();
            }

            Transaction tx = new InvocationTransaction
            {
                Version = 0,
                Script = applicationScript,
                Outputs = outputs,
                Inputs = inputs,
                Attributes = new TransactionAttribute[]
                {
                    new TransactionAttribute
                    {
                        Usage = TransactionAttributeUsage.Script,
                        Data = from.ToArray()//附加人的 Script Hash
                    }
                }
            };

            //Sign in wallet 生成附加人的签名

            var context = new ContractParametersContext(tx);
            byte[] additionalSignature = new byte[0];

            foreach (var hash in context.ScriptHashes)
            {
                if (hash == from)
                {
                    var account = wallet.GetAccount(hash);
                    if (account?.HasKey != true) continue;

                    var key = account.GetKey();
                    additionalSignature = context.Verifiable.Sign(key);
                    break;
                }
            }

            byte[] additionalVerificationScript;
            using (var sb = new ScriptBuilder())
            {
                sb.EmitPush(additionalSignature);
                additionalVerificationScript = sb.ToArray();
            }

            var witness = new Witness
            {
                InvocationScript = applicationScript,
                VerificationScript = SGAS_Contract
            };

            var additionalWitness = new Witness
            {
                InvocationScript = additionalVerificationScript,
                VerificationScript = "2103b3de29534e892a19f22cd5a58fbd1ebac782cd10743955a6b857a83cd2f90f48ac".HexToBytes()
            };

            var witnesses = new Witness[2] { witness, additionalWitness };
            tx.Scripts = witnesses.ToList().OrderBy(p => p.ScriptHash).ToArray();

            DumpValues(tx);

            return tx;
        }

        /// <summary>
        /// Verify
        /// </summary>
        /// <param name="wallet">Wallet</param>
        /// <param name="value">Value</param>
        /// <param name="txMint">Tx mint</param>
        /// <returns>Transaction</returns>
        public Transaction Verify(Wallet wallet, Fixed8 value, Transaction txMint)
        {
            var inputs = new CoinReference[]
            {
                new CoinReference()
                {
                    PrevHash = txMint.Hash,
                    PrevIndex = 0 // one for simplify
                }
            };

            var outputs = new TransactionOutput[]
            {
                new TransactionOutput()
                {
                    AssetId = AssetId, //Asset Id, this is GAS
                    ScriptHash = SGAS_ContractHash, //SGAS 地址
                    Value = value //Value
                }
            };

            var verificationScript = new byte[0];
            using (var sb = new ScriptBuilder())
            {
                sb.EmitPush(2);
                sb.EmitPush("1");
                verificationScript = sb.ToArray();
            }

            var witness = new Witness
            {
                InvocationScript = verificationScript,
                //未部署的合约不能执行 Storage.Get() 方法，所以要将合约部署，而不是调用本地的 AVM 文件
                VerificationScript = SGAS_Contract
            };

            var tx = new ContractTransaction
            {
                Version = 0,
                Outputs = outputs,
                Inputs = inputs,
                Attributes = new TransactionAttribute[0],
                Scripts = new Witness[] { witness }
            };

            return SignTx(wallet, tx);
        }
    }
}