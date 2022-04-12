using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Ccf.Ck.Libs.ResolverExpression
{
    public class ResolverRunner<ResolverValue, ResolverContext>  where ResolverValue: new() {
        private ResolverRunner() {
            IsValid = true;
        }
        
        private List<ResolverDelegate<ResolverValue, ResolverContext>> _program = new List<ResolverDelegate<ResolverValue, ResolverContext>>();
        /// <summary>
        /// If the compilation fails this is false
        /// </summary>
        /// <returns></returns>
        public bool IsValid {get; private set;}
        /// <summary>
        /// On unsuccessful compilation contains the error text (IsValid is false)
        /// </summary>
        /// <returns></returns>
        public string ErrorText {get;private set;}

        private IList<ResolverValue> DumpDataStack(Stack<ResolverValue> d) {
            if (d == null) return new List<ResolverValue>();
            List<ResolverValue> result = new List<ResolverValue>(d.Count+1);
            result.AddRange(d);
            return result;
        }
        /// <summary>
        /// The raw core Evaluator of the compiled expression, all other methods call this one and it is recommended to use one of them - 
        /// the one that matches best your scenario. RawEvaluate serves all scenarios and returns dump of the contents of the datastack. This
        /// means - it alway assumes multiple results may be returned. It will not apply any checks - everything is left for the scenario specific methods.
        /// If you want to create new kind of scenario you should use this method and not the scenario specific methods supplied by the runner. Future
        /// changes aremore likely to cause changes in their signature than in the raw core method.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="ex"></param>
        /// <param name="value"></param>
        /// <param name="name"></param>
        /// <param name="callerargs"></param>
        /// <returns></returns>
        public ResolverResult<ResolverValue> RawEvaluate(
            ResolverContext ctx, 
            out Exception ex, 
            ResolverValue value = default(ResolverValue), 
            ResolverValue name = default(ResolverValue),
            IList<ResolverValue> callerargs = null
            ) {
                Stack<ResolverValue> _dataStack = new Stack<ResolverValue>();
                ex = null;
                ResolverArguments<ResolverValue> args = new ResolverArguments<ResolverValue>(options, callerargs, value, name);
                
                for (int i = 0; i < _program.Count; i++) {
                    if (args.StopExecution) {
                        return new ResolverResult<ResolverValue>(DumpDataStack(_dataStack), args);
                    }
                    ResolverDelegate<ResolverValue, ResolverContext> instruction = _program[i];
                    if (_dataStack.Count >= instruction.ArgumentsCount) {
                        args.Clear();
                        for (int j = 0; j < instruction.ArgumentsCount; j++) {
                            args.Insert(0, _dataStack.Pop());
                        }
                        try {
                            _dataStack.Push(instruction.Invoke(ctx, args));
                        } catch (Exception e) {
                            ex = e;
                            // ??? We probably should add some condition that requires this to be ab empty value.
                            return new ResolverResult<ResolverValue>(DumpDataStack(_dataStack), args);
                        }
                    } else {
                        ex = new ArgumentException("Not enough arguments.");
                        return new ResolverResult<ResolverValue>(DumpDataStack(_dataStack), args);
                    }
            }
            return new ResolverResult<ResolverValue>(DumpDataStack(_dataStack), args); // If it is empty and this is an error is determined outside
        }
        public ResolverValue EvaluateScalar(
            ResolverContext ctx, 
            out Exception ex, 
            ResolverValue value = default(ResolverValue), 
            ResolverValue name = default(ResolverValue),  
            IList<ResolverValue> callerargs = null,
            bool strict = false) {
                var result = RawEvaluate(
                    ctx: ctx,
                    ex: out ex,
                    value: value,
                    name: name,
                    callerargs: callerargs);
                if (result.Result.Count > 0) {
                    if (strict && result.Result.Count > 1) {
                        ex = new Exception("Too many results have been produced by the expression - expected 1.");
                    }
                    return result.Result.Last();
                } else {
                    if (ex == null) {
                        ex = new Exception("There is no result to return.");
                    }
                    return new ResolverValue();
                }
        }
        public ResolverValue EvaluateScalarStrict(
            ResolverContext ctx, 
            out Exception ex, 
            ResolverValue value = default(ResolverValue), 
            ResolverValue name = default(ResolverValue),  
            IList<ResolverValue> callerargs = null) {
                var result = EvaluateScalar(
                    ctx: ctx,
                    ex: out ex,
                    value: value,
                    name: name,
                    callerargs: callerargs,
                    strict: true);
                return result;
        }
        public IList<ResolverValue> EvaluateVector(
            ResolverContext ctx, 
            out Exception ex, 
            ResolverValue value = default(ResolverValue), 
            ResolverValue name = default(ResolverValue),  
            IList<ResolverValue> callerargs = null
        ) {
           return RawEvaluate(
                    ctx: ctx,
                    ex: out ex,
                    value: value,
                    name: name,
                    callerargs: callerargs).Result; 
        }
        public ResolverResult<ResolverValue> EvaluateValidation(
            ResolverContext ctx, 
            out Exception ex, 
            ResolverValue value = default(ResolverValue), 
            ResolverValue name = default(ResolverValue),  
            IList<ResolverValue> callerargs = null
        ) {
            var result = RawEvaluate(
                    ctx: ctx,
                    ex: out ex,
                    value: value,
                    name: name,
                    callerargs: callerargs); 
            return result;
        }

        /// <summary>
        /// To minimize unneeded exception handling the exceptions are reported and the caller
        /// can decide what to do - throw them or perform its own error handling
        ///
        /// </summary>
        /// <param name="ctx">Context of the operation - up to the caller, we just pass this around.</param>
        /// <param name="ex">Output exception if error occurs</param>
        /// <param name="strict">By default this is false. If true the Evaluate will error if there are more than one results produced.</param>
        /// <returns></returns>
        // public ResolverValue Evaluate(ResolverContext ctx, out Exception ex, bool strict = false, ResolverValue value = default(ResolverValue), IList<ResolverValue> callerargs = null) {
        // }
        #region options set through the constructor by the compiler
        // Not all of these are used to the fullest at this point
        public ResolverOptions options {get;private set;}
        #endregion

        #region debugging
        public IList<string> Instructions { 
            get {
                if (_program == null || _program.Count == 0) {
                    return new List<string>() { "#empty_program"};
                }
                return _program.Select(i => string.Format("{0}({1})",i.Name, i.ArgumentsCount)).ToList();
            }
        }
        public string DumpInstructions() {
            StringBuilder sb = new StringBuilder();
            foreach (var s in Instructions) {
                sb.AppendLine(s);
            }
            return sb.ToString();
        }
        #endregion

        /// <summary>
        /// A constructor for runners - the only way to create them (usually done by the ResolverExpression compiler)
        /// </summary>
        public class ResolverRunnerConstructor {
            private ResolverRunner<ResolverValue, ResolverContext> _runner = new ResolverRunner<ResolverValue, ResolverContext>();
            public ResolverRunnerConstructor(ResolverOptions options = ResolverOptions.Default) {
                _runner.options = options;
            }
            
            public ResolverRunnerConstructor Add(ResolverDelegate<ResolverValue,ResolverContext> d) {
                _runner._program.Add(d);
                return this;
            }
            /// <summary>
            /// Completes and returns the runner.
            /// </summary>
            /// <param name="err">Optional (null). If not empty returns invalid and possibly incomplete runner. </param>
            /// <returns></returns>
            public ResolverRunner<ResolverValue, ResolverContext> Complete(string err = null) {
                var  r = _runner;
                _runner = new ResolverRunner<ResolverValue, ResolverContext>();
                if (err != null) {
                    r.ErrorText = err;
                    r.IsValid = false;
                }
                return r;
            }

        }

    }

}