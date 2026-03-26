using System;

namespace WaveFunction
{
    public class UnfortunateSeedException : SystemException
    {
        private static string DefaultMessage => "Seed was not able to make a full grid";

        public UnfortunateSeedException() : base(DefaultMessage)
        {
        }

        public UnfortunateSeedException(string message) : base(message)
        {
        }
    }
}