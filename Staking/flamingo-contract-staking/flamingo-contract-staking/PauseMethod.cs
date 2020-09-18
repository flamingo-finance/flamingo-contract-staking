using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System.ComponentModel;

namespace flamingo_contract_staking
{
    public partial class StakingContract : SmartContract
    {
        private static readonly byte[] pausePrefix = new byte[] { 0x09, 0x01 };

        [DisplayName("pause")]
        public static bool Pause(byte[] adminAddress) 
        {
            if (Runtime.CheckWitness(adminAddress) && IsAdmin(adminAddress))
            {
                Storage.Put(pausePrefix, new byte[] { 0x01 });
                return true;
            }
            else 
            {
                return false;
            }
        }

        [DisplayName("isPaused")]
        public static bool IsPaused()
        {
            var result = Storage.Get(pausePrefix);
            if (result.Length != 0) return true;
            return false;
        }

        [DisplayName("unpause")]
        public static bool Unpause(byte[] adminAddress) 
        {
            if (Runtime.CheckWitness(adminAddress) && IsAdmin(adminAddress))
            {
                Storage.Put(pausePrefix, new byte[0]);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
