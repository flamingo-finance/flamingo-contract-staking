using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System.ComponentModel;

namespace flamingo_contract_staking
{
    public partial class StakingContract : SmartContract
    {
        private static readonly byte[] pauseStakingPrefix = new byte[] { 0x09, 0x01 };
        private static readonly byte[] pauseRefundPrefix = new byte[] { 0x09, 0x02 };
        private static readonly byte[] pausePrefix = new byte[] { 0x09, 0x03 };

        [DisplayName("pause")]
        public static bool Pause(byte[] adminAddress) 
        {
            if (Runtime.CheckWitness(adminAddress) && IsAdmin(adminAddress))
            {
                PauseStaking(adminAddress);
                PauseRefund(adminAddress);
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

        public static bool PauseStaking(byte[] adminAddress) 
        {
            if (Runtime.CheckWitness(adminAddress) && IsAdmin(adminAddress))
            {
                Storage.Put(pauseStakingPrefix, new byte[] { 0x01 });
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsStakingPaused()
        {
            var result = Storage.Get(pauseStakingPrefix);
            if (result.Length != 0) return true;
            return false;
        }

        public static bool unpauseStaking(byte[] adminAddress) 
        {
            if (Runtime.CheckWitness(adminAddress) && IsAdmin(adminAddress))
            {
                Storage.Put(pauseStakingPrefix, new byte[0]);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool PauseRefund(byte[] adminAddress)
        {
            if (Runtime.CheckWitness(adminAddress) && IsAdmin(adminAddress))
            {
                Storage.Put(pauseRefundPrefix, new byte[] { 0x01 });
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool unpauseRefund(byte[] adminAddress)
        {
            if (Runtime.CheckWitness(adminAddress) && IsAdmin(adminAddress))
            {
                Storage.Put(pauseRefundPrefix, new byte[0]);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsRefundPaused()
        {
            var result = Storage.Get(pauseRefundPrefix);
            if (result.Length != 0) return true;
            return false;
        }

        [DisplayName("unpause")]
        public static bool Unpause(byte[] adminAddress) 
        {
            if (Runtime.CheckWitness(adminAddress) && IsAdmin(adminAddress))
            {
                unpauseStaking(adminAddress);
                unpauseRefund(adminAddress);
                Storage.Put(pausePrefix, new byte[0] );
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
