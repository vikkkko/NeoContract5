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

        /// <summary>
        /// Asset id, GAS
        /// </summary>
        public readonly UInt256 AssetId;

        /// <summary>
        /// Hash of SGAS
        /// </summary>
        public readonly UInt160 SGAS_ContractHash;

        /// <summary>
        /// Full source of SGAS
        /// </summary>
        public readonly byte[] SGAS_Contract;

        /// <summary>
        /// RPC Client
        /// </summary>
        public readonly RpcClient RPC;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rpc">RPC</param>
        /// <param name="contract">Contract hash (SGAS)</param>
        /// <param name="token">Token hash (GAS)</param>
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
        /// <param name="verify">Verify</param>
        /// <returns>Return signed transaction</returns>
        public Transaction SignTx(Wallet wallet, Transaction tx, bool verify)
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
                Console.WriteLine("  > Sign Fail");
            }

            DumpValues(tx, verify);

            return tx;
        }

        /// <summary>
        /// Verify transaction with wallet
        /// </summary>
        /// <param name="tx">Transaction</param>
        /// <param name="verify">Verify</param>
        /// <returns>Return signed transaction</returns>
        public void DumpValues(Transaction tx, bool verify)
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
            Console.WriteLine("  > Verify Transaction: " + (!verify ? "[skipped]" : tx.Verify(new List<Transaction> { tx }).ToString()));
            //Console.WriteLine("  > Raw Transaction: " + tx.ToArray().ToHexString());

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

            if (outputTxIndex == ushort.MaxValue) throw new Exception("Invalid TX Output");

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

            TransactionOutput[] outputs;

            if (sendValue == originalOutput.Value)
            {
                outputs = new TransactionOutput[]{ new TransactionOutput()
                {
                    AssetId = AssetId, // Asset Id, this is GAS
                    ScriptHash = SGAS_ContractHash, // SGAS
                    Value = sendValue // sendValue
                } };
            }
            else
            {
                outputs = new TransactionOutput[]{ new TransactionOutput()
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
            }

            Transaction tx;

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(SGAS_ContractHash, "mintTokens");
                sb.Emit(OpCode.THROWIFNOT);

                // Should change the hash

                byte[] nonce = new byte[8];
                Random rand = new Random();
                rand.NextBytes(nonce);
                sb.Emit(OpCode.RET, nonce);

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

            return SignTx(wallet, tx, true);
        }

        /// <summary>
        /// Refund
        /// </summary>
        /// <param name="wallet">Wallet</param>
        /// <param name="inputTx">Input tx</param>
        /// <returns>Transaction</returns>
        public Transaction Refund(Wallet wallet, Transaction inputTx)
        {
            // -------------------------------------
            // Values
            // -------------------------------------

            //if (inputTx.Outputs.Length != 1 || inputTx.Inputs.Length != 1)
            //{
            //    // SC FAIL !
            //    return null;
            //}

            var outputTxIndex = ushort.MaxValue;
            var from = wallet.GetAccounts().FirstOrDefault();

            if (inputTx != null)
            {
                // Search for the index

                for (ushort x = 0; x < inputTx.Outputs.Length; x++)
                {
                    if (inputTx.Outputs[x].ScriptHash.ToString() == SGAS_ContractHash.ToString())
                    {
                        outputTxIndex = x;
                        break;
                    }
                }
            }

            if (outputTxIndex == ushort.MaxValue) throw new Exception("Invalid TX Output");

            var originalOutput = inputTx.Outputs[outputTxIndex];

            // -------------------------------------

            var inputs = new CoinReference[]
            {
                new CoinReference()
                {
                    PrevHash = inputTx.Hash,
                    PrevIndex = outputTxIndex
                }
            };

            TransactionOutput[] outputs;
            var sendValue = originalOutput.Value;

            if (sendValue == originalOutput.Value)
            {
                outputs = new TransactionOutput[]{ new TransactionOutput()
                {
                    AssetId = AssetId, // Asset Id, this is GAS
                    ScriptHash = SGAS_ContractHash, // SGAS
                    Value = sendValue // sendValue
                } };
            }
            else
            {
                outputs = new TransactionOutput[]{ new TransactionOutput()
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
            }

            //var outputs = new TransactionOutput[]
            //{
            //    new TransactionOutput()
            //    {
            //        AssetId = AssetId, //Asset Id, this is GAS
            //        ScriptHash = SGAS_ContractHash, //SGAS 地址
            //        Value = originalOutput.Value //Value
            //    }
            //};

            byte[] applicationScript;
            using (var sb = new ScriptBuilder())
            {
                sb.EmitAppCall(SGAS_ContractHash, "refund", from.ScriptHash);
                sb.Emit(OpCode.THROWIFNOT);
                applicationScript = sb.ToArray();
            }

            byte[] applicationScriptForVerify;
            using (var sb = new ScriptBuilder())
            {
                sb.EmitPush(2);
                sb.EmitPush("1");

                applicationScriptForVerify = sb.ToArray();
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
                        Data = from.ScriptHash.ToArray() // 附加人的 Script Hash
                    }
                }
            };

            //Sign in wallet 生成附加人的签名

            var context = new ContractParametersContext(tx);
            var additionalSignature = new byte[0];

            foreach (var hash in context.ScriptHashes.Where(u => u == from.ScriptHash))
            {
                var key = from.GetKey();
                additionalSignature = context.Verifiable.Sign(key);
            }

            byte[] additionalVerificationScript;
            using (var sb = new ScriptBuilder())
            {
                sb.EmitPush(additionalSignature);
                additionalVerificationScript = sb.ToArray();
            }

            // SmartContract verification

            Witness witness = new Witness
            {
                InvocationScript = applicationScriptForVerify,
                VerificationScript = new byte[0] // SGAS_Contract
            },

            // sign of your wallet

            additionalWitness = new Witness
            {
                InvocationScript = additionalVerificationScript,
                VerificationScript = from.Contract.Script
            };

            var witnesses = new Witness[] { witness, additionalWitness };
            tx.Scripts = witnesses.ToList().OrderBy(p => p.ScriptHash).ToArray();

            DumpValues(tx, false);

            return tx;
        }

        /// <summary>
        /// Verify
        /// </summary>
        /// <param name="wallet">Wallet</param>
        /// <param name="txMint">Tx mint</param>
        /// <returns>Transaction</returns>
        public Transaction Verify(Wallet wallet, Transaction inputTx)
        {
            var from = wallet.GetAccounts().FirstOrDefault();

            var inputs = new CoinReference[]
            {
                new CoinReference()
                {
                    PrevHash = inputTx.Hash,
                    PrevIndex = 0 // Force one only
                }
            };

            var outputs = new TransactionOutput[]
            {
                new TransactionOutput()
                {
                    AssetId = AssetId, // Asset Id, this is GAS
                    ScriptHash = from.ScriptHash, // From (wallet)
                    Value = inputTx.Outputs[0].Value // Value
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
                VerificationScript = new byte[0] // SGAS_Contract
            };

            var tx = new ContractTransaction
            {
                Version = 0,
                Outputs = outputs,
                Inputs = inputs,
                Attributes = new TransactionAttribute[0],
                Scripts = new Witness[] { witness }
            };

            return SignTx(wallet, tx, false);
        }
    }
}