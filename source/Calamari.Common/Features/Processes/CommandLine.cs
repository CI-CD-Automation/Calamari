using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Calamari.Util;

namespace Calamari.Integration.Processes
{
    public class CommandLine
    {
        readonly string executable;
        readonly Func<string[], int> func;
        List<Arg> args = new List<Arg>();
        string action;
        bool useDotnet;

        public CommandLine(Func<string[], int> func)
        {
            this.func = func;
        }

        public CommandLine(string executable)
        {
            this.executable = executable ?? throw new ArgumentNullException(nameof(executable));
        }

        public CommandLine Action(string actionName)
        {
            if (action != null) throw new InvalidOperationException("Action is already set");
            action = actionName ?? throw new ArgumentNullException(nameof(actionName));
            args.Insert(0, Arg.MakeRaw(actionName));
            return this;
        }

        public CommandLine Argument(string argument)
        {
            args.Add(Arg.MakePositional(argument));
            return this;
        }
        
        public CommandLine Flag(string flagName)
        {
            args.Add(Arg.MakeFlag(flagName));
            return this;
        }

        public CommandLine UseDotnet()
        {
            useDotnet = true;
            return this;
        }

        public CommandLine PositionalArgument(object argValue)
        {
            args.Add(Arg.MakePositional(argValue));
            return this;
        }

        public CommandLine Argument(string argName, object argValue)
        {
            args.Add(Arg.MakeArg(argName, argValue));
            return this;
        }

        public CommandLineInvocation Build()
        {
            var argLine = new List<Arg>();

            var actualExe = executable;

            if (useDotnet){
                argLine.Add(Arg.MakePositional(executable));
                actualExe = "dotnet";
            }

            argLine.AddRange(args);

            return new CommandLineInvocation(actualExe, argLine.Select(b => b.Build(true)).ToArray());
        }
        
        public LibraryCallInvocation BuildLibraryCall()
        {
            return new LibraryCallInvocation(func, args.Select(b => b.Build(false)).ToArray());
        }

        public string[] GetRawArgs()
        {
            return args.SelectMany(b => b.Raw()).ToArray();
        }

        class Arg
        {
            private string Name { get; set; }
            private object Value { get; set; }
            private bool Flag { get; set; }
            private bool IsRaw { get; set; }

            public static Arg MakeArg(string name , object value)
            {
                return new Arg(){Name = name, Value = value};
            }

            public static Arg MakePositional(object value)
            {
                return new Arg() {Value = value, Flag = false };
            }
            
            public static Arg MakeFlag(string flag)
            {
                return new Arg() {Name = flag, Flag = true};
            }
            
            public static Arg MakeRaw(string value)
            {
                return new Arg() {Name = value, IsRaw = true};
            }

            public string[] Raw()
            {
                if (Flag || IsRaw || string.IsNullOrWhiteSpace(Name))
                {
                    return new []{ Build(false) };
                }

                return new[] {$"-{Normalize(Name)}", GetValue(false)};
            }
            
            public string Build(bool escapeArg)
            {
                if (Flag)
                {
                    return $"-{Normalize(Name)}";
                } else if (IsRaw)
                {
                    return Normalize(Name);
                }
                
                var sval = GetValue(escapeArg);

                return string.IsNullOrWhiteSpace(Name) ? sval : $"-{Normalize(Name)} {sval}";
            }

            static string Normalize(string text)
            {
                if (text == null) throw new ArgumentNullException(nameof(text));
                return text.Trim();
            }
            
            private string GetValue(bool escapeArg)
            {
                var sval = "";
                if (Value is IFormattable f)
                    sval = f.ToString(null, CultureInfo.InvariantCulture);
                else if (Value != null)
                    sval = Value.ToString();

                sval = Escape(sval, escapeArg);
                return sval;
            }

            string Escape(string argValue, bool escapeArg)
            {
                if (argValue == null) throw new ArgumentNullException("argValue");
                if (!escapeArg)
                    return argValue;

                // Though it isn't aesthetically pleasing, we always return a double-quoted
                // value.

                var last = argValue.Length - 1;
                var preq = true;
                while (last >= 0)
                {
                    // Escape backslashes only when they're butted up against the
                    // end of the value, or an embedded double quote

                    var cur = argValue[last];
                    if (cur == '\\' && preq)
                    {
                        argValue = argValue.Insert(last, "\\");
                    }
                    else if (cur == '"')
                    {
                        preq = true;
                    }
                    else
                    {
                        preq = false;
                    }
                    last -= 1;
                }

#if WORKAROUND_FOR_EMPTY_STRING_BUG
// linux under bash on netcore empty "" gets eaten, hand "\0"
// which gets through as a null string
            if(argValue == "")
                argValue = "\0";
#endif
                // Double-quotes are always escaped.
                return "\"" + argValue.Replace("\"", "\\\"") + "\"";
            }
        }
    }
}