using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ObjectPrinting.Extensions;

namespace ObjectPrinting.Configs
{
    public class PrintingConfig<TOwner>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly HashSet<Type> finalTypes = TypesConfig.FinalTypes;

        private readonly SerializationSettings serializationSettings = new();

        private List<object> visited = new();

        public string PrintToString(TOwner obj)
        {
            visited = new List<object>();
            return PrintToString(obj, 0);
        }

        private string PrintToString(object obj, int nestingLevel)
        {
            if (obj == null)
                return "null" + Environment.NewLine;

            if (finalTypes.Contains(obj.GetType()))
                return obj + Environment.NewLine;

            if (obj is ICollection collection)
                return PrintCollection(collection, nestingLevel);

            visited.Add(obj);
            var indentation = new string('\t', nestingLevel + 1);
            var type = obj.GetType();
            var sb = new StringBuilder();
            sb.AppendLine(type.Name);

            var printedMembers = GetFieldsAndProperties(type)
                .Where(x => !serializationSettings.IsExcluded(x))
                .Select(x => $"{indentation}{PrintMember(x, obj, nestingLevel + 1)}");

            return AppendCollection(sb, printedMembers).ToString();
        }

        private string PrintCollection(ICollection collection, int nestingLevel)
        {
            if (collection.Count == 0)
                return $"[]{Environment.NewLine}";
            var indentation = new string('\t', nestingLevel);
            var sb = new StringBuilder();
            if (nestingLevel != 0)
                sb.AppendLine();
            sb.AppendLine($"{indentation}[");
            foreach (var el in collection) sb.Append(indentation + "\t" + PrintToString(el, nestingLevel + 1));
            sb.AppendLine($"{indentation}]");

            return sb.ToString();
        }

        private string PrintMember(MemberInfo memberInfo, object obj, int nestingLevel)
            => $"{memberInfo.Name} = {Print(memberInfo, obj, nestingLevel)}";

        private string Print(MemberInfo memberInfo, object obj, int nestingLevel)
        {
            var memberValue = memberInfo.GetValue(obj);

            if (!memberInfo.GetMemberType().IsValueType && memberValue is not null)
            {
                if (visited.Exists(o => ReferenceEquals(o, memberValue)))
                    return serializationSettings.AreCycleReferencesAllowed
                        ? $"![Cyclic reference]!{Environment.NewLine}"
                        : throw new Exception("Unexpected cycle reference");

                visited.Add(memberValue);
            }

            if (serializationSettings.TryGetSerializer(memberInfo, out var serializer))
                return $"{serializer(memberValue)}{Environment.NewLine}";

            return PrintToString(memberValue, nestingLevel + 1);
        }

        public PrintingConfig<TOwner> Exclude<TType>()
        {
            serializationSettings.Exclude(typeof(TType));
            return this;
        }

        public PrintingConfig<TOwner> Exclude<TMember>(Expression<Func<TOwner, TMember>> selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            serializationSettings.Exclude(SelectMember(selector));
            return this;
        }

        public MemberConfig<TOwner, TMember> Use<TMember>(Expression<Func<TOwner, TMember>> selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            return new MemberConfig<TOwner, TMember>(this, serializationSettings, SelectMember(selector));
        }

        public TypeConfig<TOwner, TMember> Use<TMember>() => new(this, serializationSettings);

        public PrintingConfig<TOwner> UseCycleReference(bool cycleReferencesAllowed = false)
        {
            serializationSettings.AreCycleReferencesAllowed = cycleReferencesAllowed;
            return this;
        }

        private IEnumerable<MemberInfo> GetFieldsAndProperties(Type type) =>
            type.GetFields()
                .Cast<MemberInfo>()
                .Concat(type.GetProperties());

        private MemberInfo SelectMember<TType>(Expression<Func<TOwner, TType>> memberSelector)
        {
            if (memberSelector.Body is not MemberExpression memberExpression)
                throw new ArgumentException("Cannot resolve member expression");
            var memberInfo = memberExpression.Member;
            if (memberInfo is null)
                throw new ArgumentException("Cannot resolve member type");
            if (memberInfo.MemberType is not MemberTypes.Field and not MemberTypes.Property)
                throw new ArgumentException($"Expected Field or Property, but was {memberInfo.MemberType}");
            return memberInfo;
        }

        private static StringBuilder AppendCollection<T>(StringBuilder sb, IEnumerable<T> items)
        {
            foreach (var item in items)
                sb.Append(item);

            return sb;
        }
    }
}