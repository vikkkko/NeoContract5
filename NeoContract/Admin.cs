using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using Helper = Neo.SmartContract.Framework.Helper;

namespace SGAS
{
    public static class Admin
    {
        private static readonly byte[] superAdmin = Helper.ToScriptHash("APMx9jcruEG8zQdBn3sFteS4dzGyrGxkrc");
        /// <summary>
        /// 合约升级
        /// </summary>
        public static bool Migrate(object[] args)
        {
            if (args.Length != 1 && args.Length != 9)
                return false;
            var currentScript = Blockchain.GetContract(ExecutionEngine.ExecutingScriptHash).Script; //0.1
            var newScript = (byte[])args[0];
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
}
