using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Refer.Tests
{
    [TestClass]
    public class ReferenceTest
    {
        private class Baz
        {
            internal String Moo { get; set; }
        }

        private class Bar
        {
            internal Baz[] Bazzes { get; set; }
        }

        private class Doo
        {
            internal Int32 Daz { get; set; }
        }

        private class Ran
        {
            private Int32 Index { get; }

            private Ran(Int32 index)
            {
                Index = index;
            }

            public static implicit operator Ran(Int32 index)
            {
                return new Ran(index);
            }

            public override Boolean Equals(Object obj)
            {
                return (obj as Ran)?.Index == Index;
            }

            public override Int32 GetHashCode()
            {
                return Index.GetHashCode();
            }
        }

        private class Roo
        {
            internal Double Raz { get; set; }
        }

        private class Foo
        {
            internal Bar Bar { get; set; }
            internal IList<Doo> Dar { get; set; }
            internal IDictionary<Ran, Roo> Rar { get; set; }
            internal Int32 Qoo;
        }

        private static Foo MakeFoo()
            => new Foo()
            {
                Bar = new Bar()
                {
                    Bazzes = new[]
                    {
                        new Baz() {Moo = "frist"},
                        new Baz() {Moo = "sceond"},
                        new Baz() {Moo = "thrid"},
                        new Baz() {Moo = "frouth"},
                    },
                },
                Qoo = 15,
                Dar = new List<Doo>()
                {
                    new Doo() {Daz = 1},
                    new Doo() {Daz = 2},
                    new Doo() {Daz = 3},
                    new Doo() {Daz = 4},
                },
                Rar = new Dictionary<Ran, Roo>()
                {
                    [0] = new Roo() {Raz = 1.0},
                    [1] = new Roo() {Raz = 1.1},
                    [2] = new Roo() {Raz = 1.2},
                    [3] = new Roo() {Raz = 1.3},
                },
            };

        private dynamic Dyn = new Bar()
        {
            Bazzes = new []
            {
                new Baz()
                {
                    Moo = "This",
                },
                new Baz()
                {
                    Moo = "That",
                },
            }
        };

        private class Indexer
        {
            internal Object this[Object _]
            {
                get => Value;
                set => Value = value;
            }

            private Object Value;
        }

        private static void CheckValidGet<TProp>(IReference<TProp> reference, TProp control)
        {
            Assert.IsTrue(reference.Valid);
            Assert.IsTrue(((IReference) reference).Valid);
            Assert.AreEqual(control, reference.ValueOrDefault);
            Assert.AreEqual(control, reference.Value);
        }

        private static void CheckInvalidGet<TProp>(IReference<TProp> reference)
        {
            Assert.IsFalse(reference.Valid);
            Assert.AreEqual(default, reference.ValueOrDefault);
            try
            {
                var value = reference.Value;
                Assert.Fail($"Expected to throw: {reference} -> {value}");
            }
            catch (InvalidReferenceException)
            {
            }
        }

        private static void CheckValidSet<TProp>(IReference<TProp> reference, TProp value, Func<TProp> check)
        {
            reference.Value = value;
            Assert.AreEqual(value, check());
        }

        private static void CheckInvalidSet<TProp>(IReference<TProp> reference, TProp value)
        {
            try
            {
                reference.Value = value;
                Assert.Fail($"Expected to throw: {reference} -> {reference.Value}");
            }
            catch (InvalidReferenceException)
            {
            }
        }

        [TestMethod]
        public void Reference_Get_Valid()
        {
            var foo = MakeFoo();

            CheckValidGet(new Reference<Foo, Foo>(f => f, foo), foo);
            CheckValidGet(new Reference<Foo, Bar>(f => f.Bar, foo), foo.Bar);
            CheckValidGet(new Reference<Foo, Int32>(f => f.Qoo, foo), foo.Qoo);
            CheckValidGet(new Reference<Foo, Baz[]>(f => f.Bar.Bazzes, foo), foo.Bar.Bazzes);
            CheckValidGet(new Reference<Foo, IList<Doo>>(f => f.Dar, foo), foo.Dar);
            CheckValidGet(new Reference<Foo, IDictionary<Ran, Roo>>(f => f.Rar, foo), foo.Rar);
            foreach (var i in Enumerable.Range(0, 4))
            {
                CheckValidGet(new Reference<Foo, Baz>(f => f.Bar.Bazzes[i], foo), foo.Bar.Bazzes[i]);
                CheckValidGet(new Reference<Foo, String>(f => f.Bar.Bazzes[i].Moo, foo), foo.Bar.Bazzes[i].Moo);
                CheckValidGet(new Reference<Foo, Doo>(f => f.Dar[i], foo), foo.Dar[i]);
                CheckValidGet(new Reference<Foo, Int32>(f => f.Dar[i].Daz, foo), foo.Dar[i].Daz);
                CheckValidGet(new Reference<Foo, Roo>(f => f.Rar[i], foo), foo.Rar[i]);
                CheckValidGet(new Reference<Foo, Double>(f => f.Rar[i].Raz, foo), foo.Rar[i].Raz);
            }
        }

        [TestMethod]
        public void Reference_Get_Invalid()
        {
            CheckValidGet(new Reference<Foo, Foo>(f => f), null);
            CheckInvalidGet(new Reference<Foo, Bar>(f => f.Bar));
            CheckInvalidGet(new Reference<Foo, Int32>(f => f.Qoo));
            CheckInvalidGet(new Reference<Foo, IList<Doo>>(f => f.Dar));
            CheckInvalidGet(new Reference<Foo, IDictionary<Ran, Roo>>(f => f.Rar));
            var foo = new Foo();
            CheckInvalidGet(new Reference<Foo, Baz[]>(f => f.Bar.Bazzes, foo));
            foo.Bar = new Bar();
            CheckInvalidGet(new Reference<Foo, Baz>(f => f.Bar.Bazzes[2], foo));
            foo.Bar.Bazzes = new Baz[4];
            CheckInvalidGet(new Reference<Foo, String>(f => f.Bar.Bazzes[2].Moo, foo));
            foo.Dar = new List<Doo>() {null, null, null, null};
            CheckInvalidGet(new Reference<Foo, Int32>(f => f.Dar[2].Daz, foo));
            foo.Rar = new Dictionary<Ran, Roo>() {[0] = null, [1] = null, [2] = null, [3] = null};
            CheckInvalidGet(new Reference<Foo, Double>(f => f.Rar[2].Raz, foo));
        }

        [TestMethod]
        public void Reference_Set_Valid()
        {
            var foo = new Foo();
            var fooControl = MakeFoo();

            CheckValidSet(new Reference<Foo, Bar>(f => f.Bar, foo), fooControl.Bar, () => foo.Bar);
            foo.Bar = new Bar();
            CheckValidSet(new Reference<Foo, Int32>(f => f.Qoo, foo), fooControl.Qoo, () => foo.Qoo);
            CheckValidSet(new Reference<Foo, Baz[]>(f => f.Bar.Bazzes, foo), fooControl.Bar.Bazzes, () => foo.Bar.Bazzes);
            foo.Bar.Bazzes = new Baz[4];
            CheckValidSet(new Reference<Foo, IList<Doo>>(f => f.Dar, foo), fooControl.Dar, () => foo.Dar);
            foo.Dar = new List<Doo>() {null, null, null, null};
            CheckValidSet(new Reference<Foo, IDictionary<Ran, Roo>>(f => f.Rar, foo), fooControl.Rar, () => foo.Rar);
            foo.Rar = new Dictionary<Ran, Roo>() {[0] = null, [1] = null, [2] = null, [3] = null};
            foreach (var i in Enumerable.Range(0, 4))
            {
                CheckValidSet(new Reference<Foo, Baz>(f => f.Bar.Bazzes[i], foo), fooControl.Bar.Bazzes[i], () => foo.Bar.Bazzes[i]);
                foo.Bar.Bazzes[i] = new Baz();
                CheckValidSet(new Reference<Foo, String>(f => f.Bar.Bazzes[i].Moo, foo), fooControl.Bar.Bazzes[i].Moo, () => foo.Bar.Bazzes[i].Moo);
                CheckValidSet(new Reference<Foo, Doo>(f => f.Dar[i], foo), fooControl.Dar[i], () => foo.Dar[i]);
                foo.Dar[i] = new Doo();
                CheckValidSet(new Reference<Foo, Int32>(f => f.Dar[i].Daz, foo), fooControl.Dar[i].Daz, () => foo.Dar[i].Daz);
                CheckValidSet(new Reference<Foo, Roo>(f => f.Rar[i], foo), fooControl.Rar[i], () => foo.Rar[i]);
                foo.Rar[i] = new Roo();
                CheckValidSet(new Reference<Foo, Double>(f => f.Rar[i].Raz, foo), fooControl.Rar[i].Raz, () => foo.Rar[i].Raz);
            }
        }

        [TestMethod]
        public void Reference_Set_Invalid()
        {
            CheckInvalidSet(new Reference<Foo, Bar>(f => f.Bar), new Bar());
            CheckInvalidSet(new Reference<Foo, Int32>(f => f.Qoo), 15);
            CheckInvalidSet(new Reference<Foo, IList<Doo>>(f => f.Dar), new List<Doo>());
            CheckInvalidSet(new Reference<Foo, IDictionary<Ran, Roo>>(f => f.Rar), new Dictionary<Ran, Roo>());
            var foo = new Foo();
            CheckInvalidSet(new Reference<Foo, Baz[]>(f => f.Bar.Bazzes, foo), new Baz[4]);
            foo.Bar = new Bar();
            foreach (var i in Enumerable.Range(0, 4))
            {
                CheckInvalidSet(new Reference<Foo, Baz>(f => f.Bar.Bazzes[i], foo), new Baz());
                CheckInvalidSet(new Reference<Foo, Doo>(f => f.Dar[i], foo), new Doo());
                CheckInvalidSet(new Reference<Foo, Roo>(f => f.Rar[i], foo), new Roo());
            }

            foo.Bar.Bazzes = new Baz[4];
            foo.Dar = new List<Doo>() {null, null, null, null};
            foo.Rar = new Dictionary<Ran, Roo>() {[0] = null, [1] = null, [2] = null, [3] = null};
            foreach (var i in Enumerable.Range(0, 4))
            {
                CheckInvalidSet(new Reference<Foo, String>(f => f.Bar.Bazzes[i].Moo, foo), "");
                CheckInvalidSet(new Reference<Foo, Double>(f => f.Rar[i].Raz, foo), 1.0);
                CheckInvalidSet(new Reference<Foo, Int32>(f => f.Dar[i].Daz, foo), 0);
            }
        }

        [TestMethod]
        public void Reference_Indexer()
        {
            var indexer = new Indexer();

            var reference = indexer.Bind(i => i[null]);
            Assert.AreEqual(null, reference.Value);
            reference.Value = "anything";
            Assert.AreEqual("anything", reference.Value);
        }

        [TestMethod]
        public void Reference_FactoryConstructor()
        {
            var foo = MakeFoo();

            var genericReference = Reference.Create(f => f.Bar.Bazzes[2].Moo, foo);
            Assert.AreEqual("thrid", genericReference.Value);
            genericReference.Value = "thriteen";
            Assert.AreEqual("thriteen", foo.Bar.Bazzes[2].Moo);

            var basedReference = Reference<Foo>.Create(f => f.Bar.Bazzes[2].Moo, foo);
            Assert.AreEqual("thriteen", basedReference.Value);
            basedReference.Value = "froutheen";
            Assert.AreEqual("froutheen", foo.Bar.Bazzes[2].Moo);

            var basedPropertyReference = Reference<Foo, String>.Create(f => f.Bar.Bazzes[2].Moo, foo);
            Assert.AreEqual("froutheen", basedPropertyReference.Value);
            basedPropertyReference.Value = "ffitheen";
            Assert.AreEqual("ffitheen", foo.Bar.Bazzes[2].Moo);
        }

        [TestMethod]
        public void Reference_ExtensionMethodConstructor()
        {
            var foo = MakeFoo();

            var reference = foo.Bind(f => f.Bar.Bazzes[3].Moo);
            Assert.AreEqual("frouth", reference.Value);
            reference.Value = "froutheen";
            Assert.AreEqual("froutheen", foo.Bar.Bazzes[3].Moo);
        }

        [TestMethod]
        public void Reference_IReferenceValue_Valid()
        {
            var foo = MakeFoo();

            var reference = (IReference) foo.Bind(f => f.Qoo);
            Assert.AreEqual(15, reference.Value);
            reference.Value = 13;
            Assert.AreEqual(13, foo.Qoo);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidReferenceException), "Reference could not be retrieved.")]
        public void Reference_IReferenceValue_Get_Invalid()
        {
            var foo = new Foo();

            var reference = (IReference) foo.Bind(f => f.Bar.Bazzes[0].Moo);
            var _ = reference.Value;
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidCastException))]
        public void Reference_IReferenceValue_Set_Invalid()
        {
            var foo = new Foo();

            var reference = (IReference) foo.Bind(f => f.Bar.Bazzes[0].Moo);
            reference.Value = 13;
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidCastException))]
        public void Reference_IReferenceValue_Set_InvalidCast()
        {
            var foo = new Foo();

            var reference = (IReference) foo.Bind(f => f.Qoo);
            reference.Value = "test";
        }

        [TestMethod]
        public void Reference_IReferenceValueOrDefault_Valid()
        {
            var foo = new Foo();

            var reference = (IReference) foo.Bind(f => f.Bar.Bazzes[0].Moo);
            Assert.AreEqual(null, reference.ValueOrDefault);
        }

        [TestMethod]
        public void Reference_IReferenceValueOrDefault_Invalid()
        {
            var foo = new Foo();

            var reference = (IReference) foo.Bind(f => f.Bar.Bazzes[0].Moo);
            Assert.AreEqual(null, reference.ValueOrDefault);
        }

        [TestMethod]
        public void Reference_ImplicitConversion()
        {
            var foo = MakeFoo();

            var reference = foo.Bind(f => f.Qoo);
            float value = reference;
            Assert.AreEqual(15.0f, value);
        }

        [TestMethod]
        public void Reference_IModelReference_Model()
        {
            var reference = (IModelReference) new Reference<Int32, Int32>(i => i + 1);

            var foo = 3;
            reference.Model = foo;

            Assert.AreEqual(4, reference.Value);
            Assert.AreEqual(foo, reference.Model);

            var bar = 6;
            var referenceBase = (IModelReference<Int32>) reference;
            referenceBase.Model = bar;

            Assert.AreEqual(7, referenceBase.Value);
            Assert.AreEqual(bar, referenceBase.Model);

            var baz = -1;
            var referenceBaseProp = (IModelReference<Int32, Int32>) reference;
            referenceBaseProp.Model = baz;

            Assert.AreEqual(0, referenceBaseProp.Value);
            Assert.AreEqual(baz, referenceBaseProp.Model);
        }

        [TestMethod]
        public void Reference_GetValue()
        {
            var reference = new Reference<Int32, Int32>(i => i + 1);

            Assert.AreEqual(4, reference.GetValue(3));
            Assert.AreEqual(4, ((IModelReference) reference).GetValue(3));
            Assert.AreEqual(4, ((IModelReference<Int32>) reference).GetValue(3));
            Assert.AreEqual(4, ((IModelReference<Int32, Int32>) reference).GetValue(3));
        }

        [TestMethod]
        public void Reference_GetValueOrDefault_Valid()
        {
            var reference = new Reference<Int32, Int32>(i => i + 1);

            Assert.AreEqual(4, reference.GetValueOrDefault(3));
            Assert.AreEqual(4, ((IModelReference) reference).GetValueOrDefault(3));
            Assert.AreEqual(4, ((IModelReference<Int32>) reference).GetValueOrDefault(3));
            Assert.AreEqual(4, ((IModelReference<Int32, Int32>) reference).GetValueOrDefault(3));
        }

        [TestMethod]
        public void Reference_GetValueOrDefault_Invalid()
        {
            var reference = new Reference<Int32, Int32>(i => i + ((Int32?) null).Value);

            Assert.AreEqual(0, reference.GetValueOrDefault(3));
            Assert.AreEqual(0, ((IModelReference) reference).GetValueOrDefault(3));
            Assert.AreEqual(0, ((IModelReference<Int32>) reference).GetValueOrDefault(3));
            Assert.AreEqual(0, ((IModelReference<Int32, Int32>) reference).GetValueOrDefault(3));
        }

        [TestMethod]
        public void Reference_SetValue()
        {
            var foo = new Foo()
            {
                Qoo = 3,
            };

            var reference = new Reference<Foo, Int32>(f => f.Qoo);
            Assert.AreEqual(3, foo.Qoo);
            reference.SetValue(foo, 4);
            Assert.AreEqual(4, foo.Qoo);

            var bar = new Foo()
            {
                Qoo = 1,
            };

            reference.Model = bar;
            Assert.AreEqual(1, bar.Qoo);
            reference.SetValue(foo, 7);
            Assert.AreEqual(1, bar.Qoo);
            Assert.AreEqual(7, foo.Qoo);

            ((IModelReference) reference).SetValue(foo, 7);
            Assert.AreEqual(1, bar.Qoo);
            Assert.AreEqual(7, foo.Qoo);

            ((IModelReference<Foo>) reference).SetValue(foo, 7);
            Assert.AreEqual(1, bar.Qoo);
            Assert.AreEqual(7, foo.Qoo);

            ((IModelReference<Foo, Int32>) reference).SetValue(foo, 7);
            Assert.AreEqual(1, bar.Qoo);
            Assert.AreEqual(7, foo.Qoo);
        }

        [TestMethod]
        public void Reference_ReferenceValueType_Get()
        {
            var reference = new Reference<Int32, Int32>(i => i + 1);

            Assert.AreEqual(typeof(Int32), reference.ValueType);
            Assert.AreEqual(typeof(Int32), ((IReference) reference).ValueType);
            Assert.AreEqual(typeof(Int32), ((IReference<Int32>) reference).ValueType);
            Assert.AreEqual(typeof(Int32), ((IModelReference) reference).ValueType);
            Assert.AreEqual(typeof(Int32), ((IModelReference<Int32>) reference).ValueType);
            Assert.AreEqual(typeof(Int32), ((IModelReference<Int32, Int32>) reference).ValueType);
        }

        [TestMethod]
        public void Reference_DynamicReference_Get()
        {
            var reference = this.Bind(t => t.Dyn);

            Assert.AreEqual("That", reference.Value.Bazzes[1].Moo);
        }

        [TestMethod]
        public void Reference_DynamicReference_Set()
        {
            var reference = this.Bind(t => t.Dyn);

            reference.Value.Bazzes[1].Moo = "Those";

            Assert.AreEqual("Those", Dyn.Bazzes[1].Moo);

            reference.Value = 123;

            Assert.AreEqual(123, Dyn);
        }
    }
}
