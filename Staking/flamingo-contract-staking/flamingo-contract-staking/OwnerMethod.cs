using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System.ComponentModel;

namespace flamingo_contract_staking
{
    public partial class StakingContract : SmartContract
    {
        private static readonly byte[] originOwner = "AW5fekjC9VdWG6Jy2P2xfNQVWUvNh39A6c".ToScriptHash();
        private static readonly byte[] adminPrefix = new byte[] { 0x03, 0x01 };

        [DisplayName("addadmin")]
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

        [DisplayName("removeadmin")]
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

        [DisplayName("isadmin")]
        public static bool IsAdmin(byte[] admin) 
        {
            if (admin.Equals(originOwner)) return true;
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
