﻿using System;
using System.Collections.Generic;
using System.Linq;
using Rethought.Optional;

namespace Rethought.Commands.Parser.Auto
{
    public class AutoTypeParser<TInput, TOutput> : ITypeParser<TInput, TOutput>
    {
        // TODO make configurable
        private const char GroupEnclosingChar = '"';

        private readonly Dictionary<Type, ITypeParser<string, object>> dictionary;
        private readonly System.Func<TInput, string> func;

        public AutoTypeParser(
            Dictionary<Type, ITypeParser<string, object>> dictionary,
            System.Func<TInput, string> func)
        {
            this.dictionary = dictionary;
            this.func = func;
        }

        public Option<TOutput> Parse(TInput input)
        {
            var message = func.Invoke(input);

            var type = typeof(TOutput);

            var commandParameter = SplitInput(message);

            if (!type.GetConstructors().FirstOrNone().TryGetValue(out var constructorInfo))
            {
                throw new InvalidOperationException($"{nameof(TOutput)} contains no valid constructor.");
            };

            var parsedParameters = new List<object>();
            var parameterInfos = constructorInfo.GetParameters();

            var skippedParameters = 0;
            for (var index = 0; index < parameterInfos.Length; index++)
            {
                var constructorParameter = parameterInfos[index];

                var constructorParameterIsOption = false;

                if (constructorParameter.ParameterType.IsGenericType)
                    constructorParameterIsOption = constructorParameter.ParameterType.GetGenericTypeDefinition() == typeof(Option<>);

                if (constructorParameter.ParameterType == typeof(TInput))
                {
                    parsedParameters.Add(input);
                    skippedParameters++;
                    continue;
                }

                if (commandParameter.ElementAtOrNone(index - skippedParameters).TryGetValue(out var inputParameter))
                {
                    type = constructorParameter.ParameterType;
                    if (constructorParameterIsOption)
                        type = constructorParameter.ParameterType.GetGenericArguments().First();


                    var typeParser = dictionary[type];
                    var typeParserResultOption = typeParser.Parse(inputParameter);

                    if (typeParserResultOption.TryGetValue(out var typeParserResult))
                    {
                        if (constructorParameterIsOption)
                        {

                            var methodInfo = typeof(Option<>).GetMethods().FirstOrNone(x => x.Name == "Some" && x.GetGenericArguments().Length == 1);

                            if (methodInfo.TryGetValue(out var value))
                            {
                                parsedParameters.Add(value.MakeGenericMethod(type).Invoke(null, new []{ typeParserResult }));

                            }
                        }
                        else
                        {
                            parsedParameters.Add(typeParserResult);

                        }
                    }
                    else if (constructorParameterIsOption)
                    {
                        parsedParameters.Add(null);
                    }
                    else
                    {
                        return default;
                    }
                }
                else if (constructorParameterIsOption)
                {
                    parsedParameters.Add(null);
                }
                else
                {
                    return default;
                }
            }

            var instance = constructorInfo.Invoke(parsedParameters.ToArray());

            return (TOutput) instance;
        }

        /// <summary>
        /// Splits the input into individual parameters. Everything inside a <see cref="GroupEnclosingChar"/> is considered as one parameter.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        private static IReadOnlyList<string> SplitInput(string input)
        {
            return input.Split(GroupEnclosingChar)
                .Select(
                    (element, index) =>
                        index % 2 == 0
                            ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            : new[] { element })
                .SelectMany(element => element)
                .ToList();
        }
    }
}