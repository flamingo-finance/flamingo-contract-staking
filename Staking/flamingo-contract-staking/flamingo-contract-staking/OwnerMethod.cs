using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System.ComponentModel;

namespace flamingo_contract_staking
{
    public partial class StakingContract : SmartContract
    {
        private static readonly byte[] InitialOwnerScriptHash = "AW5fekjC9VdWG6Jy2P2xfNQVWUvNh39A6c".ToScriptHash();
        private static readonly byte[] adminPrefix = new byte[] { 0x03, 0x01 };
        private static readonly byte[] OwnerPrefix = new byte[] { 0x03, 0x02 };

        [DisplayName("getOwner")]
        public static byte[] GetOwner() 
        {
            var owner = Storage.Get(OwnerPrefix);
            if (owner.Length == 0)
            {
                return InitialOwnerScriptHash;
            }
            else 
            {
                return owner;
            }
        }

        [DisplayName("setOwner")]
        public static bool SetOwner(byte[] ownerAddress) 
        {
            if (!Runtime.CheckWitness(GetOwner()) || ownerAddress.Length != 20) return false;
            Storage.Put(OwnerPrefix, ownerAddress);
            return true;
        }

        [DisplayName("addAdmin")]
        public static bool AddAdmin(byte[] admin)
        {
            if (Runtime.CheckWitness(GetOwner()))
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

        [DisplayName("removeAdmin")]
        public static bool RemoveAdmin(byte[] admin) 
        {
            if (Runtime.CheckWitness(GetOwner()))
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

        [DisplayName("isAdmin")]
        public static bool IsAdmin(byte[] admin) 
        {
            if (admin.Equals(GetOwner())) return true;
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
