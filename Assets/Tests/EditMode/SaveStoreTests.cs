using System.IO;
using BubbleAscent.Core;
using NUnit.Framework;

namespace BubbleAscent.Tests
{
    public class SaveStoreTests
    {
        string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "bubble-ascent-save-tests-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void RoundTrip_KeepsBestHeight_AndVersion()
        {
            var data = new SaveData { bestHeight = 123.5f };
            SaveData back = SaveStore.Parse(SaveStore.Serialize(data));
            Assert.AreEqual(123.5f, back.bestHeight);
            Assert.AreEqual(SaveData.CurrentVersion, back.version);
        }

        [Test]
        public void Parse_EmptyOrCorrupt_GivesFreshData()
        {
            Assert.AreEqual(0f, SaveStore.Parse(null).bestHeight);
            Assert.AreEqual(0f, SaveStore.Parse("").bestHeight);
            Assert.AreEqual(0f, SaveStore.Parse("{ not json").bestHeight);
            Assert.AreEqual(0f, SaveStore.Parse("[1,2,3]").bestHeight);
        }

        [Test]
        public void Parse_OldVersion_IsMigrated_AndBadValuesCleaned()
        {
            SaveData old = SaveStore.Parse("{\"version\":0,\"bestHeight\":42}");
            Assert.AreEqual(SaveData.CurrentVersion, old.version);
            Assert.AreEqual(42f, old.bestHeight);

            Assert.AreEqual(0f, SaveStore.Parse("{\"version\":1,\"bestHeight\":-5}").bestHeight);
        }

        [Test]
        public void SaveThenLoad_FromDisk_AndOverwrite()
        {
            string path = Path.Combine(_dir, SaveStore.FileName);
            Assert.AreEqual(0f, SaveStore.Load(path).bestHeight, "missing file");

            Assert.IsTrue(SaveStore.Save(path, new SaveData { bestHeight = 10f }));
            Assert.AreEqual(10f, SaveStore.Load(path).bestHeight);

            Assert.IsTrue(SaveStore.Save(path, new SaveData { bestHeight = 20f }));
            Assert.AreEqual(20f, SaveStore.Load(path).bestHeight);
            Assert.IsFalse(File.Exists(path + ".tmp"), "temp file swapped in");
        }

        [Test]
        public void Load_Unreadable_GivesFreshData_AndSaysDoNotOverwrite()
        {
            string path = Path.Combine(_dir, SaveStore.FileName);
            SaveStore.Save(path, new SaveData { bestHeight = 99f });

            // Another handle holds the file exclusively: reading throws.
            using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                SaveData data = SaveStore.Load(path, out bool readable);
                Assert.IsFalse(readable);
                Assert.AreEqual(0f, data.bestHeight);
            }

            SaveData after = SaveStore.Load(path, out bool readableAfter);
            Assert.IsTrue(readableAfter);
            Assert.AreEqual(99f, after.bestHeight, "nothing was overwritten");

            SaveStore.Load(Path.Combine(_dir, "none.json"), out bool missingReadable);
            Assert.IsTrue(missingReadable, "a missing file is fine to create");
        }

        [Test]
        public void Load_CorruptFile_GivesFreshData()
        {
            string path = Path.Combine(_dir, SaveStore.FileName);
            File.WriteAllText(path, "garbage");
            Assert.AreEqual(0f, SaveStore.Load(path).bestHeight);
        }
    }
}
