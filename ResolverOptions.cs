using System;


namespace Ccf.Ck.Libs.ResolverExpression
{
    [Flags]
        public enum ResolverOptions {
            none=0x0000,
            RecurseName = 0x0001,
            RecurseValue = 0x0002,
            RecurseCallData = 0x04,
            Validator = 0x0010,
            Default = 0x0002
        }
}