

namespace Ccf.Ck.Libs.ResolverExpression
{
    public interface IResolverFinder<ResolverValue, ResolverContext> where ResolverValue: new() {
        ResolverDelegate<ResolverValue, ResolverContext> GetResolver(string name);
    }
}