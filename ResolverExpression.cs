using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;

namespace Ccf.Ck.Libs.ResolverExpression
{
    public abstract class ResolverExpression<ResolverValue, ResolverContext> where ResolverValue: new() {

        #region Resolver suppliers
        /// <summary>
        /// Asked for resolver delegate by name during the compilation of expression.
        /// </summary>
        /// <param name="name"> The name of the resolver delegate needed</param>
        /// <param name="finder">External find helper passed during compilation (see compile methods). The finder should be the same and the availability of the delegates returned should not depend on the specific execution circumstances.</param>
        /// <returns></returns>
        protected abstract ResolverDelegate<ResolverValue, ResolverContext> GetResolver(string name, IResolverFinder<ResolverValue, ResolverContext> finder = null);
        protected abstract ResolverDelegate<ResolverValue, ResolverContext> PushInt(int v);
        protected abstract ResolverDelegate<ResolverValue, ResolverContext> PushDouble(double v);
        protected abstract ResolverDelegate<ResolverValue, ResolverContext> PushString(string v);
        protected abstract ResolverDelegate<ResolverValue, ResolverContext> PushNull();
        protected abstract ResolverDelegate<ResolverValue, ResolverContext> PushBool(bool Value);
        protected abstract ResolverDelegate<ResolverValue, ResolverContext> PushValue();
        protected abstract ResolverDelegate<ResolverValue, ResolverContext> PushName();
        protected abstract ResolverDelegate<ResolverValue, ResolverContext> PushParam(string name);
        protected abstract ResolverDelegate<ResolverValue, ResolverContext> ValidationChecker();
    
        #endregion

        /// <summary>
        /// Level 0 recognition
        /// </summary>
        enum Terms {
            
            none = 0,
            // Space is found - usually ignored
            space = 1,
            // special literals for specific values - true, false, null, value
            specialliteral = 2,
            // identifier - function name or parameter name to fetch (the actual fetching depends on the usage)
            identifier = 3,
            // Open normal bracket (function call arguments, grouping is not supported intentionally - see the docs for more details)
            openbracket = 4,
            // close normal bracket - end of function call argument list.
            closebracket = 5,
            // string literal 'something'
            stringliteral = 6,
            // numeric literal like: 124, +234, -324, 123.45, -2.43, +0.23423 etc.
            numliteral = 7,
            // comma separator of arguments. can be used at top level also, in this case this will produce multiple results (usable only with the corresponding evaluation routines)
            comma = 8,
            // end of the expression
            end = 9
        }
        /// <summary>
        /// Level 1 recognition - the index is also the priority (we take a little shortcut here)
        /// </summary>

        private struct OpEntry {
            internal OpEntry(string v, Terms t,int p = -1) {
                Value = v;
                Term = t;
                Pos = p;
            }
            internal string Value;
            internal Terms Term;
            internal int Pos;

        }
        
        private static readonly Regex _regex = new Regex(@"(\s+)|(true|false|null|value|name)|([a-zA-Z_][a-zA-Z0-9_\.\-]*)|(\()|(\))|(?:\'((?:\\'|[^\'])*)\')|([\+\-]?\d+(?:\.\d*)?)|(\,)|($)",
            RegexOptions.Multiline);
        public ResolverExpression() {
            
        }

        #region Internal helpers (can be implemented as local methods, but I hate it)
        private OpEntry NoEntry() {
            return new OpEntry(null,Terms.none);
        }
        private bool IsEntryEmpty(OpEntry e) {
            return (e.Term == Terms.none);
        }
        private int ParsePos(Match m) {
            return m.Index;
        }
        private string ReportError(string fmt,Match m = null) {
            if (m != null) {
                return string.Format(fmt,ParsePos(m));
            } 
            return fmt;
        }
        private string ReportError(string fmt,int m = -1) {
            if (m >= 0) {
                return string.Format(fmt,m);
            } 
            return fmt;
        }
        #endregion
        public ResolverRunner<ResolverValue, ResolverContext> CompileValidationExpression(string expr, IResolverFinder<ResolverValue, ResolverContext> finder = null) {
            return Compile(expr, ResolverOptions.Validator | ResolverOptions.RecurseValue, finder);
        }
        public ResolverRunner<ResolverValue, ResolverContext> CompileResolverExpression(string expr, IResolverFinder<ResolverValue, ResolverContext> finder = null) {
            return Compile(expr, ResolverOptions.Default, finder);
        }
        public ResolverRunner<ResolverValue, ResolverContext> Compile(string _intext, ResolverOptions options, IResolverFinder<ResolverValue, ResolverContext> finder = null) {
            bool forValidation = (options & ResolverOptions.Validator) != 0;
            Stack<OpEntry> opstack = new Stack<OpEntry>();
            ResolverRunner<ResolverValue, ResolverContext>.ResolverRunnerConstructor runner = new ResolverRunner<ResolverValue, ResolverContext>.ResolverRunnerConstructor();
            OpEntry undecided = new OpEntry();
            OpEntry entry; // Temp var
            int pos = 0; // used and updated only for additional error checking. The algorithm does not depend on this.
            int level = 0;

            Match match = _regex.Match(_intext);
            while(match.Success) {
                if (pos != match.Index) return runner.Complete(ReportError("Syntax error at {0} - unrecognized text",match.Index));
                pos = match.Index + match.Length;
                if (match.Groups[0].Success) {
                    for (int i = 1; i < match.Groups.Count; i++) {
                        if (match.Groups[i].Success) {
                            string curval = match.Groups[i].Value;
                            switch ((Terms)i) {
                                case Terms.identifier:
                                    if (!IsEntryEmpty(undecided)) {
                                        return runner.Complete(ReportError("Syntax error at {0}.", match));
                                    }
                                    undecided = new OpEntry(curval,Terms.identifier,match.Index);
                                goto nextTerm;
                                case Terms.openbracket:
                                    if (!IsEntryEmpty(undecided) && undecided.Term == Terms.identifier) {
                                        opstack.Push(undecided); // Function call
                                        undecided = NoEntry();
                                    }
                                    level ++;
                                goto nextTerm;
                                case Terms.closebracket:
                                    if (!IsEntryEmpty(undecided) && undecided.Term == Terms.identifier) {
                                        runner.Add(PushParam(undecided.Value));
                                        undecided = NoEntry();
                                    }
                                    // *** Function call
                                    if (opstack.Count == 0) return runner.Complete(ReportError("Syntax error - function call has no function name at {0}",match));
                                    entry = opstack.Pop();
                                    if (entry.Term == Terms.identifier) {
                                        var _resolver = GetResolver(entry.Value, finder);
                                        if (_resolver != null) {
                                            runner.Add(GetResolver(entry.Value, finder));
                                        } else {
                                            return runner.Complete(ReportError($"Resolver not found - {entry.Value} does not exist at {{0}}",match));    
                                        }
                                    } else {
                                        return runner.Complete(ReportError("Syntax error - function call has no function name at {0}",match));
                                    }
                                    level --;
                                goto nextTerm;
                                case Terms.comma:
                                    if (undecided.Term == Terms.identifier) {
                                        runner.Add(PushParam(undecided.Value));
                                        undecided = NoEntry();
                                    } else if (!IsEntryEmpty(undecided)) { // If this happend it will be our mistake. Nothing but identifiers should appear in the undecided
                                        return runner.Complete(ReportError("Internal error at {0}",undecided.Pos));
                                    }
                                    if (forValidation && level == 0) {
                                        runner.Add(ValidationChecker());
                                    }
                                goto nextTerm;
                                case Terms.numliteral:
                                    if (!IsEntryEmpty(undecided)) return runner.Complete(ReportError("Syntax error at {0}",undecided.Pos));
                                    if (curval.IndexOf('.') >= 0) { // double
                                        double t;
                                        if (double.TryParse(curval,NumberStyles.Any,CultureInfo.InvariantCulture, out t)) {
                                            runner.Add(PushDouble(t));
                                        } else {
                                            return runner.Complete(ReportError("Invalid double number at {0}",match));
                                        }
                                    } else {
                                        int n;
                                        if (int.TryParse(curval,NumberStyles.Any,CultureInfo.InvariantCulture, out n)) {
                                            runner.Add(PushInt(n));
                                        } else {
                                            return runner.Complete(ReportError("Invalid double number at {0}",match));
                                        }
                                    }
                                goto nextTerm;
                                case Terms.specialliteral:
                                    if (!IsEntryEmpty(undecided)) return runner.Complete(ReportError("Syntax error at {0}",undecided.Pos));
                                    if (curval == "null") {
                                        runner.Add(PushNull());
                                    } else if (curval == "true") {
                                        runner.Add(PushBool(true));
                                    } else if (curval == "false") {
                                        runner.Add(PushBool(false));
                                    } else if (curval == "value") {
                                        runner.Add(PushValue());
                                    } else if (curval == "name") {
                                        runner.Add(PushName());
                                    } else {
                                        return runner.Complete(ReportError("Syntax error at {0}",match));
                                    }
                                    
                                goto nextTerm;
                                case Terms.stringliteral:
                                    if (!IsEntryEmpty(undecided))
                                    {
                                        return runner.Complete(ReportError("Syntax error at {0}", undecided.Pos));
                                    }
                                    runner.Add(PushString(curval));
                                goto nextTerm;
                                case Terms.space:
                                    // do nothing - we simply ignore the space
                                goto nextTerm;
                                case Terms.end:
                                    if (!IsEntryEmpty(undecided) && undecided.Term == Terms.identifier) {
                                        runner.Add(PushParam(undecided.Value));
                                        undecided = NoEntry();
                                    }
                                    if (forValidation && level == 0) {
                                        runner.Add(ValidationChecker());
                                    }
                                    if (opstack.Count == 0) {
                                        // The stack must be empty at this point
                                        return runner.Complete();
                                    } else {
                                        return runner.Complete("Syntax error at the expression end - check for matching brackets");
                                    }
                                // break;
                                default:
                                    return runner.Complete(ReportError("Syntax error at {0}",match));
                                
                            }
                        } // catch actual group
                    } // Check every possible group
                } else {
                    // Unrecognized or end
                }
                nextTerm:
                match = match.NextMatch();
            } // next term
            return null;   
        }
    }
}