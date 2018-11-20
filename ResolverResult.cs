using System.Collections.Generic;
using System.Linq;

namespace Ccf.Ck.Libs.ResolverExpression
{
    public struct ResolverResult<ResolverValue> where ResolverValue: new() {
        public ResolverResult(ResolverValue result, string message = null, object calldata = null ){
            Result = new List<ResolverValue>() {result};
            CallData = calldata;
            Message = message;
        }
        public ResolverResult(IEnumerable<ResolverValue> result, string message = null, object calldata = null ){
            Result = new List<ResolverValue>(result);
            CallData = calldata;
            Message = message;
        }
        public ResolverResult(ResolverValue result, IList<ResolverValue> args) {
            Result = new List<ResolverValue>() { result };
            ResolverArguments<ResolverValue> _args = args as ResolverArguments<ResolverValue>;
            if (_args != null) {
                CallData = _args.CallData;
                Message = _args.Message;
            } else {
                CallData = null;
                Message = null;
            }
        }
        public ResolverResult(IList<ResolverValue> result, IList<ResolverValue> args) {
            Result = result ?? new List<ResolverValue>();
            ResolverArguments<ResolverValue> _args = args as ResolverArguments<ResolverValue>;
            if (_args != null) {
                CallData = _args.CallData;
                Message = _args.Message;
            } else {
                CallData = null;
                Message = null;
            }
        }
        public ResolverResult(IEnumerable<ResolverValue> result, IList<ResolverValue> args) {
            Result = new List<ResolverValue>(result);
            ResolverArguments<ResolverValue> _args = args as ResolverArguments<ResolverValue>;
            if (_args != null) {
                CallData = _args.CallData;
                Message = _args.Message;
            } else {
                CallData = null;
                Message = null;
            }
        }
        public IList<ResolverValue> Result {get; private set;}
        public object CallData {get; private set;}
        public string Message {get; private set;}
        /// <summary>
        /// Returns the top (last) value. Useful for validation expressions and others where more than one value may remain in the data
        /// stack after the evaluation, but the last one is the one we are interested in (like in non-strict scalar evaluation).
        /// </summary>
        public ResolverValue Value {
            get {
                if (Result == null || Result.Count == 0) return new ResolverValue();
                return Result.Last();
            }
        }

    }

}