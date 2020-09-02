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
        private static readonly byte[] originOwner = "AQzRMe3zyGS8W177xLJfewRRQZY2kddMun".ToScriptHash();
        private static readonly byte[] adminPrefix = new byte[] { 0x03, 0x01 };

        public static bool AddAdmin(byte[] admin)
        {
            if (Runtime.CheckWitness(originOwner))
            {
                byte[] key = adminPrefix.Concat(admin);
                Storage.Put(key, new byte[] { 0x01 });
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool RemoveAdmin(byte[] admin) 
        {
            if (Runtime.CheckWitness(originOwner))
            {
                if (IsAdmin(admin))
                {
                    byte[] key = adminPrefix.Concat(admin);
                    Storage.Delete(key);
                    return true;
                }
                return true;
            }
            else 
            {
                return false;
            }
        }

        private static bool IsAdmin(byte[] admin) 
        {
            byte[] key = adminPrefix.Concat(admin);
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
