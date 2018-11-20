using System.Collections.Generic;

namespace Ccf.Ck.Libs.ResolverExpression
{
    /// <summary>
    /// This class builds upon IList to make it possible for the resolver delegates to reach additional data supplied by the runner and
    /// the custom implementations. It is always passed as IList, because the overwhelming majority of the resolvers do not need to access anything else
    /// but regular arguments. Usually only the special resolvers, such as PushName, PushValue and some others, need to cast the arguments to this type
    /// and lookup special values - value, name, recursions etc.
    ///
    /// In certain usages it may be necessary to have some data accessible to many resolver delegates during the whole evaluation operation - other than the 
    /// context. The calldata property is reserved for that purpose and can be employed as data holder for the call even through recursions (they usually occur
    /// through the PushParam delegate, intended for parameter fetching by executing other expressions). For more detail on the intended usage see the examples 
    /// and the documentation.
    /// </summary>
    public class ResolverArguments<ResolverValue>: List<ResolverValue> where ResolverValue: new() {
            public ResolverArguments():base() {}
            public ResolverArguments(int n):base(n) {}
            public ResolverArguments(
                ResolverOptions options, 
                IList<ResolverValue> otherargs = null,
                ResolverValue value = default(ResolverValue), 
                ResolverValue name = default(ResolverValue),
                object calldata = null):base() {
                    var _otherargs = otherargs as ResolverArguments<ResolverValue>;
                    if (_otherargs != null) {
                        this.Recursions = _otherargs.Recursions + 1;
                        this.CallData = _otherargs.CallData;
                        if (((options & ResolverOptions.RecurseValue) != 0)) {
                            this.Value = _otherargs.Value;
                        } else {
                            this.Value = value;
                        }
                        if (((options & ResolverOptions.RecurseName) != 0)) {
                            this.Name = _otherargs.Name;
                        } else {
                            this.Name = name;
                        }
                    } else {
                        this.Value = value;
                        this.Name = name;
                    }
            }
            
            #region Additional parameters
            /// <summary>
            /// Incremented each time a recursion occurs (i.e. Evaluate is called with existing ResolverArguments)
            /// </summary>
            /// <returns></returns>
            public int Recursions {get; private set;} = 0;
            /// <summary>
            /// A property for application usage. The resolver library will not use or check this, but it will keep
            /// it the same throughout the execution and recursions too.
            /// </summary>
            /// <returns></returns>
            public object CallData {get;set;} = null;
            /// <summary>
            /// Holds the value passed to the runner from the outside. The value is not transferred to recursive calls.
            /// </summary>
            /// <returns></returns>
            public ResolverValue Value {get;private set;}
            /// <summary>
            /// Acts the same way as the Value property, but is supported separately for better readability. Name should be
            /// used when multiple expressions are held in a dictionary fashion - i.e. each has a name. Most often these names
            /// can carry useful meaning beyond just naming the expression. For example they can be names of parameters in an SQL 
            /// statement the values of which has to be resolved by the expression. It is not unusual these names to follow a naming convention
            /// which implicitly specifies hints about the source from which the value is to be obtained - hence the name under which
            /// the expression is specified can be a required parameter for the expression. Thus, having the special keyword "name" provides
            /// better readability.
            /// </summary>
            /// <returns></returns>
            public ResolverValue Name {get;private set;}
            /// <summary>
            /// The resolver delegates have no direct access to the runner in order to avoid techniques that can make the execution unpredictable.
            /// Still, in some circumstances it may be desirable to have a way to complete/stop the execution in the middle. This can be achieved by
            /// setting this property to true, the runner will check it before executing the next instruction (delegate) and bail out in that case.
            /// One usage in which this is a critical feature is the validation expressions. Validation usually consists of a set several expressions
            /// separated with comas and the first unsuccessful one should stop the execution of the rest, because usually they are dependent on each other
            /// and there is no point in continuing the execution further.
            ///  </summary>
            /// <returns></returns>
            public bool StopExecution {get;set;} = false;
            /// <summary>
            /// A property for setting a message. Typically used by validator delegates.
            /// </summary>
            /// <returns></returns>
            public string Message {get;set;}
            public object Trace {get; set;} // ResolverTraceData<ResolverValue, ResolverContext> - because it is used mostly internall it does not need to be pretty 

            #endregion
        }
}        