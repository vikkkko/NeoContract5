using Neo;
using Neo.Core;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeoContract.UnitTests
{
    public static class SGASTest
    {
        //SGAS MintTokens
        public static Transaction MintTokens()
        {
            var inputs = new List<CoinReference> {
                //coin reference A
                new CoinReference(){
                    PrevHash = new UInt256("6a5b6ab39a61bab0d53295d49a07de79719ea1132a4d3b0d9c685610553bffec".HexToBytes().Reverse().ToArray()),
                    PrevIndex = 0 //21257
                }
            }.ToArray();

            var outputs = new List<TransactionOutput>{ new TransactionOutput()
            {
                AssetId = Blockchain.UtilityToken.Hash, //Asset Id, this is GAS
                ScriptHash = Wallet.ToScriptHash("AUiJLznkxE2Kt3MqF7DRCCDw49yzM5u5k1"), //SGAS 地址
                Value = new Fixed8((long)(1 * (long)Math.Pow(10, 8))) //Value
            },new TransactionOutput()
            {
                AssetId = Blockchain.UtilityToken.Hash, //Asset Id, this is GAS
                ScriptHash = Wallet.ToScriptHash("AJd31a8rYPEBkY1QSxpsGy8mdU4vTYTD4U"), //找零地址
                Value = new Fixed8((long)(21256 * (long)Math.Pow(10, 8))) //Value
            }}.ToArray();

            var scriptHash = new UInt160("0x402de1ac5f1d0e912bd458da96e11de5a196ec8d".Remove(0, 2).HexToBytes().Reverse().ToArray());
            
            //Transfer
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(scriptHash, "mintTokens");
                sb.Emit(OpCode.THROWIFNOT);

                byte[] nonce = new byte[8];
                Random rand = new Random();
                rand.NextBytes(nonce);
                sb.Emit(OpCode.RET, nonce);
                return new InvocationTransaction
                {
                    Version = 1,
                    Script = sb.ToArray(),
                    Outputs = outputs,
                    Inputs = inputs,
                    Attributes = new TransactionAttribute[0],
                    Scripts = new Witness[0]
                };
            }
        }
    }
}
