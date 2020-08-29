using System;
using System.Numerics;
using System.ComponentModel;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;

[assembly: Features(ContractPropertyState.HasStorage)]

namespace flamingo_contract_staking
{
    public class FLM : SmartContract
    {
        // TODO: replace Pika with authorized account address
        private static readonly byte[] Pika = "AWWPJDbuGiaGFcuzSt644rbvCxPTXJocoB".ToScriptHash();
        private static readonly byte[] SupplyKey = "sk".AsByteArray();
        private static readonly byte[] PikaCountKey = "pck".AsByteArray();

        private static readonly byte[] BalancePrefix = new byte[] { 0x01, 0x01 };
        private static readonly byte[] AllowancePrefix = new byte[] { 0x01, 0x02 };
        private static readonly byte[] PikaPrefix = new byte[] { 0x01, 0x03 };

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> TransferEvent;
        [DisplayName("approval")]
        public static event Action<byte[], byte[], BigInteger> ApproveEvent;
        [DisplayName("addPika")]
        public static event Action<byte[]> AddPikaEvent;
        [DisplayName("removePika")]
        public static event Action<byte[]> RemovePikaEvent;



        public static object Main(string method, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return false;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (method == "name") return Name();
                if (method == "symbol") return Symbol();
                if (method == "decimals") return Decimals();
                if (method == "totalSupply") return TotalSupply();
                if (method == "balanceOf") return BalanceOf((byte[])args[0]);
                if (method == "allowance") return Allowance((byte[])args[0], (byte[])args[1]);
                if (method == "transfer") return Transfer((byte[])args[0], (byte[])args[1], (BigInteger)args[2]);
                if (method == "approve") return Approve((byte[])args[0], (byte[])args[1], (BigInteger)args[2]);
                if (method == "transferFrom") return TransferFrom((byte[])args[0], (byte[])args[1], (byte[])args[2], (BigInteger)args[3]);
                if (method == "addPika") return AddPika((byte[])args[0]);
                if (method == "removePika") return RemovePika((byte[])args[0]);
                if (method == "isPika") return IsPika((byte[])args[0]);
                if (method == "pikaCount") return PikaCount();
                if (method == "mint") return Mint((byte[])args[0], (byte[])args[1], (BigInteger)args[2]);
            }
            throw new InvalidOperationException("Invalid method: ".AsByteArray().Concat(method.AsByteArray()).AsString());
        }

        [DisplayName("name")]
        public static string Name() => "Flamingo";

        [DisplayName("symbol")]
        public static string Symbol() => "FLM";

        [DisplayName("decimals")]
        public static byte Decimals() => 8;

        [DisplayName("totalSupply")]
        public static BigInteger TotalSupply()
        {
            return Storage.Get(SupplyKey).AsBigInteger();
        }

        [DisplayName("balanceOf")]
        public static BigInteger BalanceOf(byte[] owner)
        {
            assert(owner.Length == 20, "balanceOf: invalid owner-".AsByteArray().Concat(owner).AsString());
            return Storage.Get(BalancePrefix.Concat(owner)).AsBigInteger();
        }

        [DisplayName("allowance")]
        public static BigInteger Allowance(byte[] owner, byte[] spender)
        {
            assert(owner.Length == 20, "allowance: invalid owner-".AsByteArray().Concat(owner).AsString());
            assert(spender.Length == 20, "allowance: invalid spender-".AsByteArray().Concat(spender).AsString());
            return Storage.Get(AllowancePrefix.Concat(owner).Concat(spender)).AsBigInteger();
        }

        [DisplayName("transfer")]
        public static bool Transfer(byte[] from, byte[] to, BigInteger amt)
        {
            assert(from.Length == 20 && to.Length == 20 , "transfer: invalid from or to, from-".AsByteArray().Concat(from).Concat(" and to-".AsByteArray()).Concat(to).AsString());
            assert(amt > 0, "transfer: invalid amount-".AsByteArray().Concat(amt.ToByteArray()).AsString());
            assert(Runtime.CheckWitness(from) || from.Equals(ExecutionEngine.CallingScriptHash), "transfer: CheckWitness failed, from-".AsByteArray().Concat(from).AsString());
            assert(!from.Equals(to), "trasnfer: from-".AsByteArray().Concat(from).Concat(" eqals to-".AsByteArray()).Concat(to).AsString());

            BigInteger fromAmt = Storage.Get(BalancePrefix.Concat(from)).AsBigInteger();
            assert(fromAmt >= amt, "transfer: amount-".AsByteArray().Concat(amt.ToByteArray()).Concat(" is greater than fromAmt-".AsByteArray()).Concat(fromAmt.ToByteArray()).AsString());

            if (fromAmt == amt)
                Storage.Delete(BalancePrefix.Concat(from));
            else
                Storage.Put(BalancePrefix.Concat(from), fromAmt - amt);

            BigInteger toAmt = Storage.Get(BalancePrefix.Concat(to)).AsBigInteger();
            Storage.Put(BalancePrefix.Concat(to), toAmt + amt);

            TransferEvent(from, to, amt);

            return true;
        }


        [DisplayName("approve")]
        public static bool Approve(byte[] owner, byte[] spender, BigInteger amt)
        {
            assert(owner.Length == 20 && spender.Length == 20, "approve: invalid owner or spender, owner-".AsByteArray().Concat(owner).Concat("and spender-".AsByteArray()).Concat(spender).AsString());
            assert(amt > 0, "approve: invalid amount-".AsByteArray().Concat(amt.ToByteArray()).AsString());
            assert(Runtime.CheckWitness(owner) || owner.Equals(ExecutionEngine.CallingScriptHash), "approve: CheckWitness failed, owner-".AsByteArray().Concat(owner).AsString());

            Storage.Put(AllowancePrefix.Concat(owner).Concat(spender), amt);
            ApproveEvent(owner, spender, amt);
            return true;
        }

        [DisplayName("transferFrom")]
        public static bool TransferFrom(byte[] spender, byte[] owner, byte[] receiver, BigInteger amt)
        {
            assert(spender.Length == 20 && owner.Length == 20 && receiver.Length == 20, "transferFrom: invalid spender or owner or receiver, spender-".AsByteArray().Concat(spender).Concat(", owner-".AsByteArray()).Concat(owner).Concat(" and receiver-".AsByteArray()).Concat(receiver).AsString());
            assert(amt > 0, "transferFrom: invalid amount-".AsByteArray().Concat(amt.ToByteArray()).AsString());
            assert(Runtime.CheckWitness(spender) || owner.Equals(ExecutionEngine.CallingScriptHash), "transferFrom: CheckWitness failed, spender-".AsByteArray().Concat(spender).AsString());

            byte[] ownerKey = BalancePrefix.Concat(owner);
            BigInteger ownerAmt = Storage.Get(ownerKey).AsBigInteger();
            assert(ownerAmt >= amt, "transferFrom: Owner balance-".AsByteArray().Concat(amt.ToByteArray()).Concat(" is less than amt-".AsByteArray()).Concat(amt.ToByteArray()).AsString());

            byte[] allowanceKey = AllowancePrefix.Concat(owner).Concat(spender);
            BigInteger allowance = Storage.Get(allowanceKey).AsBigInteger();

            assert(allowance >= amt, "transferFrom: allowance-".AsByteArray().Concat(allowance.ToByteArray()).Concat(" is less than amt-".AsByteArray()).Concat(amt.ToByteArray()).AsString());

            if (amt == allowance)
            {
                Storage.Delete(allowanceKey);
                Storage.Put(BalancePrefix.Concat(owner), ownerAmt - amt);
            }
            else
            {
                Storage.Put(allowanceKey, allowance - amt);
                Storage.Put(ownerKey, ownerAmt - amt);
            }

            Storage.Put(BalancePrefix.Concat(receiver), Storage.Get(BalancePrefix.Concat(receiver)).AsBigInteger() + amt);

            TransferEvent(owner, receiver, amt);

            return true;
        }

        [DisplayName("mint")]
        public static bool Mint(byte[] pika, byte[] receiver, BigInteger amt)
        {
            assert(pika.Length == 20 && receiver.Length == 20, "mint: invalid pika or receiver, pika-".AsByteArray().Concat(pika).Concat(" and receiver-".AsByteArray()).Concat(receiver).AsString());
            assert(amt > 0, "mint: invalid amount-".AsByteArray().Concat(amt.ToByteArray()).AsString());

            assert(IsPika(pika) || pika.Equals(Pika), "mint: pika-".AsByteArray().Concat(pika).Concat(" is not a real pika".AsByteArray()).AsString());
            assert(Runtime.CheckWitness(pika) || pika.Equals(ExecutionEngine.CallingScriptHash), "mint: CheckWitness failed, pika-".AsByteArray().Concat(pika).AsString());

            Storage.Put(BalancePrefix.Concat(receiver), BalanceOf(receiver) + amt);
            Storage.Put(SupplyKey, TotalSupply() + amt);
            TransferEvent(null, receiver, amt);
            return true;
        }


        [DisplayName("addPika")]
        public static bool AddPika(byte[] newPika)
        {
            assert(Runtime.CheckWitness(Pika) && !newPika.Equals(Pika), "addPika: CheckWitness failed, only first pika can add other pika");
            assert(!IsPika(newPika), "addPika: newPika-".AsByteArray().Concat(newPika).Concat(" is already a pika".AsByteArray()).AsString());
            Storage.Put(PikaPrefix.Concat(newPika), 1);
            Storage.Put(PikaCountKey, Storage.Get(PikaCountKey).AsBigInteger() + 1);
            AddPikaEvent(newPika);
            return true;
        }

        [DisplayName("removePika")]
        public static bool RemovePika(byte[] pika)
        {
            assert(Runtime.CheckWitness(Pika) && !pika.Equals(Pika), "removePika: CheckWitness failed, only first pika can remove other pika");
            assert(IsPika(pika), "removePika: pika-".AsByteArray().Concat(pika).Concat(" is NOT a pika".AsByteArray()).AsString());
            Storage.Delete(PikaPrefix.Concat(pika));
            Storage.Put(PikaCountKey, Storage.Get(PikaCountKey).AsBigInteger() - 1);
            RemovePikaEvent(pika);
            return true;
        }

        [DisplayName("isPika")]
        public static bool IsPika(byte[] pika)
        {
            return Storage.Get(PikaPrefix.Concat(pika)).Length != 0 || pika.Equals(Pika);
        }

        [DisplayName("pikaCount")]
        public static BigInteger PikaCount()
        {
            return Storage.Get(PikaCountKey).AsBigInteger() + 1;
        }


        private static void assert(bool condition, string msg)
        {
            if (!condition)
            {
                // TODO: uncomment next line on mainnet
                //throw new InvalidOperationException("transfer: from equals to address.");
                Runtime.Notify(Symbol().AsByteArray().Concat(msg.AsByteArray()).AsString());
            }
        }

    }
}
