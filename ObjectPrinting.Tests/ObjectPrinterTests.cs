﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using ObjectPrinting.Extensions;

namespace ObjectPrinting.Tests
{
    public class ObjectPrinterTests
    {
        [Test]
        public void Should_DoNothing_When_NoSettings()
        {
            var person = PersonFactory.CreateDefaultPerson();
            var expected = PersonFactory.GetDefaultPersonPrinting(person);

            var printedPerson = ObjectPrinter.For<Person>()
                .PrintToString(person);

            printedPerson.Should().Be(expected);
        }

        [Test]
        public void Should_DoNothing_When_ExcludeTypesThatNotExistsInObject()
        {
            var person = PersonFactory.CreateDefaultPerson();
            var expected = PersonFactory.GetDefaultPersonPrinting(person);

            var printedPerson = ObjectPrinter.For<Person>()
                .Exclude<ulong>()
                .PrintToString(person);

            printedPerson.Should().Be(expected);
        }

        [Test]
        public void Should_ThrowException_WhenSelectorIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ObjectPrinter.For<Person>()
                    .Use(x => x.Age).With(null));
        }

        [Test]
        public void Should_Exclude_Properties()
        {
            var person = PersonFactory.CreateDefaultPerson();

            var printedPerson = ObjectPrinter.For<Person>()
                .Exclude(x => x.FirstName)
                .PrintToString(person);

            printedPerson.Should().NotContain(nameof(person.FirstName));
        }

        [Test]
        public void Should_Exclude_Fields()
        {
            var person = PersonFactory.CreateDefaultPerson();

            var printedPerson = ObjectPrinter.For<Person>()
                .Exclude(x => x.LastName)
                .PrintToString(person);

            printedPerson.Should().NotContain(nameof(person.LastName));
        }

        [Test]
        public void Should_Exclude_Types()
        {
            var person = PersonFactory.CreateDefaultPerson();

            var printedPerson = ObjectPrinter.For<Person>()
                .Exclude<Guid>()
                .PrintToString(person);

            printedPerson.Should().NotContain(nameof(person.Id));
        }

        [Test]
        public void Should_ThrowException_When_NotMemberSelectorProvided()
        {
            var person = PersonFactory.CreateDefaultPerson();

            void PrintedPersonAction() =>
                ObjectPrinter.For<Person>()
                    .Exclude(x => "f")
                    .PrintToString(person);

            Assert.Throws<ArgumentException>(PrintedPersonAction);
        }

        [Test]
        public void Should_Support_CustomTypeSerialization()
        {
            var person = PersonFactory.CreateDefaultPerson();

            var printedPerson = ObjectPrinter.For<Person>()
                .Use<string>().With(x => $"\"{x}\"")
                .PrintToString(person);

            printedPerson.Should().Contain($"{nameof(person.LastName)} = \"{person.LastName}\"")
                .And.Contain($"{nameof(person.FirstName)} = \"{person.FirstName}\"");
        }

        [Test]
        public void Should_Support_CustomMemberSerialization()
        {
            var person = PersonFactory.CreateDefaultPerson();

            var printedPerson = ObjectPrinter.For<Person>()
                .Use(x => x.FirstName).With(x => $"cool {x}")
                .PrintToString(person);

            printedPerson.Should().Contain($"{nameof(person.FirstName)} = cool {person.FirstName}");
        }

        [Test]
        public void MemberCustomSerializer_HasMorePriority_Than_TypeCustomSerializer()
        {
            var person = PersonFactory.CreateDefaultPerson();

            var printedPerson = ObjectPrinter.For<Person>()
                .Use<string>().With(x => $"not cool {x}")
                .Use(x => x.FirstName).With(x => $"cool {x}")
                .PrintToString(person);

            printedPerson.Should().Contain($"{nameof(person.FirstName)} = cool {person.FirstName}")
                .And.Contain($"{nameof(person.LastName)} = not cool {person.LastName}");
        }

        [Test]
        public void When_MemberHasCustomSerializer_TypeCustomSerializer_Doesnt_Affect()
        {
            var person = PersonFactory.CreateDefaultPerson();

            var printedPerson = ObjectPrinter.For<Person>()
                .Use(x => x.FirstName).With(x => $"cool {x}")
                .Use<string>().With(x => $"not cool {x}")
                .PrintToString(person);

            printedPerson.Should().Contain($"{nameof(person.FirstName)} = cool {person.FirstName}")
                .And.Contain($"{nameof(person.LastName)} = not cool {person.LastName}");
        }

        [Test]
        public void Should_Apply_LastConfigurationOfMember()
        {
            var person = PersonFactory.CreateDefaultPerson();
            const string firstMessage = "this won't be applied";
            const string secondMessage = "but this will";

            var printedPerson = ObjectPrinter.For<Person>()
                .Use(x => x.Age).With(_ => firstMessage)
                .Use(x => x.Age).With(_ => secondMessage)
                .PrintToString(person);

            printedPerson.Should().Contain(secondMessage)
                .And.NotContain(firstMessage);
        }

        [Test]
        public void Should_Apply_LastConfiguration()
        {
            var person = PersonFactory.CreateDefaultPerson();
            const string alternativeSerializationMessage = "crazy train";

            var printedPerson = ObjectPrinter.For<Person>()
                .Use(x => x.LastName).With(_ => alternativeSerializationMessage)
                .Use(x => x.LastName).WithTrimming(4)
                .PrintToString(person);

            printedPerson.Should().Contain($"{nameof(person.LastName)} = {person.LastName.Substring(0, 4)}")
                .And.NotContain(alternativeSerializationMessage);
        }

        [Test]
        public void Should_Apply_LastConfigurationOfType()
        {
            var person = PersonFactory.CreateDefaultPerson();
            const string firstMessage = "this won't be applied";
            const string secondMessage = "but this will";

            var printedPerson = ObjectPrinter.For<Person>()
                .Use<int>().With(_ => firstMessage)
                .Use<int>().With(_ => secondMessage)
                .PrintToString(person);

            printedPerson.Should().Contain(secondMessage)
                .And.NotContain(firstMessage);
        }

        [Test]
        public void Should_Support_CustomTypeCulture()
        {
            var culture = new CultureInfo("en-GB");
            var person = PersonFactory.CreateDefaultPerson();

            var printedPerson = ObjectPrinter.For<Person>()
                .Use<double>().With(culture)
                .PrintToString(person);

            printedPerson.Should().Contain($"{nameof(person.Height)} = {person.Height.ToString(culture)}");
        }

        [Test]
        public void Should_Support_CustomMemberCulture()
        {
            var culture = new CultureInfo("en-GB");
            var person = PersonFactory.CreateDefaultPerson();

            var printedPerson = ObjectPrinter.For<Person>()
                .Use(x => x.Age).With(culture)
                .PrintToString(person);

            printedPerson.Should().Contain($"{nameof(person.Age)} = {person.Age.ToString(culture)}");
        }

        [Test]
        public void Should_Support_StringsTrimming()
        {
            var person = PersonFactory.CreateDefaultPerson();

            var printedPerson = ObjectPrinter.For<Person>()
                .Use(x => x.FirstName).WithTrimming(3)
                .PrintToString(person);

            printedPerson.Should()
                .Contain($"{nameof(person.FirstName)} = {person.FirstName.Substring(0, 3)}");
        }

        [Test]
        public void Should_DetectCyclicReferences_AndDontThrowByDefault()
        {
            var person = PersonFactory.CreatePersonWithCycleReference();

            var printedPerson = ObjectPrinter.For<Person>()
                .UseCycleReference(true)
                .PrintToString(person);

            printedPerson.Should().Contain($"{nameof(person.Parent)} = ![Cyclic reference]!");
        }

        [Test]
        public void Should_DetectCyclicReferences_AndControlIt()
        {
            var person = PersonFactory.CreatePersonWithCycleReference();

            var personPrinter = ObjectPrinter.For<Person>()
                .UseCycleReference();

            Assert.Throws<Exception>(() => personPrinter.PrintToString(person));
        }

        [Test]
        public void Should_PrintEmptyArray()
        {
            var array = Array.Empty<string>();

            var printedArray = ObjectPrinter.For<string[]>()
                .PrintToString(array);

            printedArray.Should().Be($"[]{Environment.NewLine}");
        }

        [Test]
        public void Should_PrintArray()
        {
            var cities = new[] {"Moscow", "Rio", "Zurich"};
            var expected = new StringBuilder()
                .AppendLine("[")
                .AppendLine("\tMoscow")
                .AppendLine("\tRio")
                .AppendLine("\tZurich")
                .AppendLine("]")
                .ToString();

            var printedArray = ObjectPrinter.For<string[]>()
                .PrintToString(cities);

            printedArray.Should().Be(expected);
        }

        [Test]
        public void Should_PrintList()
        {
            var cities = new List<string> {"Moscow", "Rio", "Zurich"};
            var expected = new StringBuilder()
                .AppendLine("[")
                .AppendLine("\tMoscow")
                .AppendLine("\tRio")
                .AppendLine("\tZurich")
                .AppendLine("]")
                .ToString();

            var printedArray = ObjectPrinter.For<List<string>>()
                .PrintToString(cities);

            printedArray.Should().Be(expected);
        }

        [Test]
        public void Should_PrintDictionary()
        {
            var currencies = new Dictionary<string, int>
            {
                {"RUB", 70},
                {"USD", 100}
            };
            var expected = new StringBuilder()
                .AppendLine("[")
                .AppendLine("\tKeyValuePair`2")
                .AppendLine("\t\tKey = RUB")
                .AppendLine("\t\tValue = 70")
                .AppendLine("\tKeyValuePair`2")
                .AppendLine("\t\tKey = USD")
                .AppendLine("\t\tValue = 100")
                .AppendLine("]")
                .ToString();

            var printedArray = ObjectPrinter.For<Dictionary<string, int>>()
                .PrintToString(currencies);

            printedArray.Should().Be(expected);
        }

        [Test]
        public void Should_PrintNestedCollections()
        {
            var items = new List<Dictionary<string, string>>
            {
                new()
                {
                    {"Moscow", "Russia"},
                    {"Paris", "France"}
                },
                new()
                {
                    {"London", "England"},
                    {"Madrid", "Spain"}
                }
            };

            var expected = new StringBuilder()
                .AppendLine("[")
                .AppendLine("\t")
                .AppendLine("\t[")
                .AppendLine("\t\tKeyValuePair`2")
                .AppendLine("\t\t\tKey = Moscow")
                .AppendLine("\t\t\tValue = Russia")
                .AppendLine("\t\tKeyValuePair`2")
                .AppendLine("\t\t\tKey = Paris")
                .AppendLine("\t\t\tValue = France")
                .AppendLine("\t]")
                .AppendLine("\t")
                .AppendLine("\t[")
                .AppendLine("\t\tKeyValuePair`2")
                .AppendLine("\t\t\tKey = London")
                .AppendLine("\t\t\tValue = England")
                .AppendLine("\t\tKeyValuePair`2")
                .AppendLine("\t\t\tKey = Madrid")
                .AppendLine("\t\t\tValue = Spain")
                .AppendLine("\t]")
                .AppendLine("]")
                .ToString();

            var printedItems = ObjectPrinter.For<List<Dictionary<string, string>>>()
                .PrintToString(items);

            printedItems.Should().Be(expected);
        }
    }
}