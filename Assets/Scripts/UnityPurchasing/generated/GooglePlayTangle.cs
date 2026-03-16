// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("0awF57AkvL2/+ZbpkYrTpydkVXBg4+3i0mDj6OBg4+PiKEOR+XLvCtJg48DS7+TryGSqZBXv4+Pj5+LhZS14qbg1dsAIUdlk2x92c26fD8UMaj4cTrh6Sjp92+GdRlRxiZPTeMMQvmXUAf7fytWTxh7QXdcTg+Sof935cETIu4wFDe/kePBVILf2Mb0iS/WOV9FdxDQ9JNdJz+C5v0FjFpGKGYft0C0PKFdpKcnUbNDPfYSxLFu/sibFpPYjohYRC8eUoPPTXQveiLPZuKBYxgYxcxyjWHDSHftY8uXH3A5df/QPmGtKxse1FDrVT4p6QqSq8SxsLgqXfu4iKMsn8+sEumLIjC54k2FA8WvvNUzglT5izI4wfHxP3Thl9d9Ei+Dh4+Lj");
        private static int[] order = new int[] { 6,1,6,3,6,13,13,10,13,12,11,13,13,13,14 };
        private static int key = 226;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
