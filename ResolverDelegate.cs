using System;
using System.Collections.Generic;
using System.Linq;

namespace Ccf.Ck.Libs.ResolverExpression
{
    public class ResolverDelegate<ResolverValue, ResolverContext> where ResolverValue: new() {
        private Func<ResolverContext, IList<ResolverValue>,ResolverValue> _delegate;
        public int ArgumentsCount {get; private set;}
        public string Name {get; private set;}

        public ResolverDelegate(Func<ResolverContext, IList<ResolverValue>, ResolverValue> d, int args, string name = null ) {
            _delegate = d;
            ArgumentsCount = args;
            if (name == null) {
                if (d != null) {
                    Name = d.GetType().Name;
                } else {
                    Name = "#invalid_delegate";
                }
            } else {
                Name = name;
            }
        }
        public ResolverValue Invoke(ResolverContext ctx, IList<ResolverValue> args) {
            return _delegate(ctx, args);
        }
        public ResolverValue Invoke(ResolverContext ctx, params ResolverValue[] args) {
            return _delegate(ctx, args.ToList());
        }
    }

}