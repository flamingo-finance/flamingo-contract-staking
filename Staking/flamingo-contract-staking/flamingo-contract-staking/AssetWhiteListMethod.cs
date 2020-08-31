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
        private static byte[] assetPrefix = new byte[] { 0x04, 0x01 };
        public static bool AddAsset(byte[] assetId, byte[] adminScriptHash) 
        {
            if (Runtime.CheckWitness(adminScriptHash) && IsAdmin(adminScriptHash))
            {
                byte[] key = assetPrefix.Concat(assetId);
                Storage.Put(key, new byte[] { 0x01 });
                return true;
            }
            else 
            {
                return false;
            }
        }

        public static bool RemoveAsset(byte[] assetId, byte[] adminScriptHash) 
        {
            if (Runtime.CheckWitness(adminScriptHash) && IsAdmin(adminScriptHash))
            {
                if (IsInWhiteList(assetId))
                {
                    byte[] key = assetPrefix.Concat(assetId);
                    Storage.Delete(key);
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

        private static bool IsInWhiteList(byte[] assetId) 
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
