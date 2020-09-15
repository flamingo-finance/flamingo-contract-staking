using System;
using System.Collections.Generic;
using System.Numerics;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using Neo.SmartContract.Framework.Services.System;
using Neo.SmartContract.Framework.Services.Neo;

namespace flamingo_contract_staking
{
    public partial class StakingContract : SmartContract
    {
        private static readonly byte[] assetPrefix = new byte[] { 0x04, 0x01 };
        private static readonly byte[] assetMapPrefix = new byte[] { 0x04, 0x02 };
        public static bool AddAsset(byte[] assetId, byte[] adminScriptHash) 
        {
            if (Runtime.CheckWitness(adminScriptHash) && IsAdmin(adminScriptHash))
            {
                byte[] key = assetPrefix.Concat(assetId);
                Storage.Put(key, new byte[] { 0x01 });
                AddAssetMap(assetId);
                return true;
            }
            else 
            {
                return false;
            }
        }

        private static bool AddAssetMap(byte[] assetId) 
        {
            Map<byte[], bool> assetMap; 
            var rawAssetMap = Storage.Get(assetMapPrefix);
            if (rawAssetMap.Length == 0)
            {
                assetMap = new Map<byte[], bool>();
            }
            else 
            {
                assetMap = (Map<byte[], bool>)rawAssetMap.Deserialize();
            }            
            assetMap[assetId] = true;
            Storage.Put(assetMapPrefix, assetMap.Serialize());
            return true;
        }

        private static bool RemoveAssetMap(byte[] assetId) 
        {
            Map<byte[], bool> assetMap;
            var rawAssetMap = Storage.Get(assetMapPrefix);
            if (rawAssetMap.Length == 0)
            {
                assetMap = new Map<byte[], bool>();
            }
            else
            {
                assetMap = (Map<byte[], bool>)rawAssetMap.Deserialize();
            }
            assetMap[assetId] = false;
            Storage.Put(assetMapPrefix, assetMap.Serialize());
            return true;
        }

        public static bool RemoveAsset(byte[] assetId, byte[] adminScriptHash) 
        {
            if (Runtime.CheckWitness(adminScriptHash) && IsAdmin(adminScriptHash))
            {
                if (IsInWhiteList(assetId))
                {
                    byte[] key = assetPrefix.Concat(assetId);
                    Storage.Delete(key);
                    RemoveAssetMap(assetId);
                    return true;
                }
                else
                {
                    return true;
                }
            }
            else 
            {
                return false;
            }

        }

        public static bool IsInWhiteList(byte[] assetId) 
        {
            byte[] key = assetPrefix.Concat(assetId);
            var result = Storage.Get(key);
            if (result.Length != 0)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }
    }
}
