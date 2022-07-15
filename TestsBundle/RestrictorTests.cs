using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using NHibernate;
using NHibernate.Linq;

namespace TestsBundle
{
    [TestClass]
    public class RestrictorTester
    {
        [TestMethod]
        public void DbRestrictorTest()
        {

            var DbRestrReader = new BSImport.Restrictor.DatabaseRestrictsUpdater(BSImport.ConnectionManager.AmurDFO, new int[] { -1 });
            var DbRestrictor = new BSImport.Restrictor.Restrictor<ISessionFactory>(DbRestrReader);
            RestrictorTest(DbRestrictor);
        }

        [TestMethod]
        public void FileRestrictorTest()
        {
            var FileResrtReader = new BSImport.Restrictor.FileRestrictsUpdater("restricts.ff");
            var FileRestrictor = new BSImport.Restrictor.Restrictor<string>(FileResrtReader);
            RestrictorTest(FileRestrictor);
        }

        public void RestrictorTest<T>(BSImport.Restrictor.Restrictor<T> TheRestrictor)
        {
            using (var session = BSImport.ConnectionManager.AmurDFO.OpenSession())
            {
                var RawDbData = session.Query<BSImport.DFOEntity.Entity.ImportStation>().ToList();
                if (RawDbData.Count < 1)
                    throw new Exception("No import stations data found in the DB");
                var StrongRawDbData = RawDbData.Where(x => x.IsStrong).ToList();
                var WeakRawDbData = RawDbData.Where(x => x.IsStrong == false).ToList();
                Random Randy = new Random((RawDbData.Count * DateTime.UtcNow.Millisecond - DateTime.UtcNow.Minute) << DateTime.UtcNow.Hour);

                if (StrongRawDbData.Count > 0)
                {
                    for (int i = 0; i < StrongRawDbData.Count; i++)
                    {
                        var TmpStation = StrongRawDbData[i];
                        Assert.IsTrue(TheRestrictor.IsStrong(TmpStation.StationType, TmpStation.Code));
                    }
                }

                if (WeakRawDbData.Count > 0)
                {
                    for (int i = 0; i < WeakRawDbData.Count; i++)
                    {
                        var TmpStation = WeakRawDbData[i];
                        Assert.IsFalse(TheRestrictor.IsStrong(TmpStation.StationType, TmpStation.Code));
                    }
                }

                if (RawDbData.Count > 0)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        var TmpStation = RawDbData[Randy.Next(0, RawDbData.Count - 1)];
                        Assert.IsTrue(TheRestrictor.Approved(TmpStation.Code, TmpStation.StationType));
                    }
                }
            }
        }
    }
}
