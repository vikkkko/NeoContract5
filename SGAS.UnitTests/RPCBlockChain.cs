using System;
using System.Collections.Generic;
using Neo;
using Neo.Core;
using Neo.IO.Caching;

namespace SGAS.UnitTests
{
    public class RPCBlockChain : Blockchain
    {
        public readonly RpcClient RPC;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rpc">RPC</param>
        public RPCBlockChain(RpcClient rpc)
        {
            RPC = rpc;
        }

        public override bool IsDisposed => throw new NotImplementedException();

        public override UInt256 CurrentBlockHash => throw new NotImplementedException();

        public override UInt256 CurrentHeaderHash => throw new NotImplementedException();

        public override uint HeaderHeight => 0;

        public override uint Height => 0;

        public override bool AddBlock(Block block)
        {
            throw new NotImplementedException();
        }

        public override bool ContainsBlock(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public override bool ContainsTransaction(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public override bool ContainsUnspent(UInt256 hash, ushort index)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override AccountState GetAccountState(UInt160 script_hash)
        {
            throw new NotImplementedException();
        }

        public override AssetState GetAssetState(UInt256 asset_id)
        {
            return RPC.GetAssetState(asset_id);
        }

        public override Block GetBlock(UInt256 hash)
        {
            return null;
        }

        public override UInt256 GetBlockHash(uint height)
        {
            return UInt256.Zero;
        }

        public override ContractState GetContract(UInt160 hash)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<ValidatorState> GetEnrollments()
        {
            throw new NotImplementedException();
        }

        public override Header GetHeader(uint height)
        {
            throw new NotImplementedException();
        }

        public override Header GetHeader(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public override MetaDataCache<T> GetMetaData<T>()
        {
            throw new NotImplementedException();
        }

        public override Block GetNextBlock(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public override UInt256 GetNextBlockHash(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public override DataCache<TKey, TValue> GetStates<TKey, TValue>()
        {
            throw new NotImplementedException();
        }

        public override StorageItem GetStorageItem(StorageKey key)
        {
            throw new NotImplementedException();
        }

        public override long GetSysFeeAmount(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public override Transaction GetTransaction(UInt256 hash, out int height)
        {
            height = 0;
            return RPC.GetTransaction(hash);
        }

        public override Dictionary<ushort, SpentCoin> GetUnclaimed(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public override TransactionOutput GetUnspent(UInt256 hash, ushort index)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<TransactionOutput> GetUnspent(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public override bool IsDoubleSpend(Transaction tx)
        {
            return false;
        }

        protected override void AddHeaders(IEnumerable<Header> headers)
        {
            throw new NotImplementedException();
        }
    }
}
