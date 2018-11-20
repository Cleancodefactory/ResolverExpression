using System.Collections.Generic;

namespace Ccf.Ck.Libs.ResolverExpression
{
    public class ResolverTraceData<ResolverValue, ResolverContext> where ResolverValue: new() {

        public ResolverTraceData() {
            
        }

        public void AddStep(
            Stack<ResolverValue> datastack,
            ResolverDelegate<ResolverValue, ResolverContext> instruction,
            ResolverArguments<ResolverValue> args) {

        }
        
    }
}