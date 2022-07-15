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
            using (var session = BSImport.ConnectionManager.AmurDFO.OpenSession())
            {
                var DbRestrReader = new BSImport.Restrictor.DatabaseRestrictsUpdater(BSImport.ConnectionManager.AmurDFO, new int[] { -1 });
                var DbRestrictor = new BSImport.Restrictor.Restrictor<ISessionFactory>(DbRestrReader);

                var RawDbData = session.Query<BSImport.DFOEntity.Entity.ImportStation>().ToList();
                if (RawDbData.Count < 1)
                    throw new Exception("No import stations data found in the DB");
                var StrongRawDbData = RawDbData.Where(x => x.IsStrong).ToList();
                var WeakRawDbData = RawDbData.Where(x => x.IsStrong == false).ToList();
                Random Randy = new Random((RawDbData.Count * DateTime.UtcNow.Millisecond - DateTime.UtcNow.Minute) << DateTime.UtcNow.Hour);

                if (StrongRawDbData.Count > 0)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var TmpStation = StrongRawDbData[Randy.Next(0, StrongRawDbData.Count - 1)];
                        Assert.IsTrue(DbRestrictor.IsStrong(TmpStation.StationType, TmpStation.Code));
                    }
                }

                if (WeakRawDbData.Count > 0)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var TmpStation = WeakRawDbData[Randy.Next(0, WeakRawDbData.Count - 1)];
                        Assert.IsFalse(DbRestrictor.IsStrong(TmpStation.StationType, TmpStation.Code));
                    }
                }

                if (RawDbData.Count > 0)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        var TmpStation = RawDbData[Randy.Next(0, RawDbData.Count - 1)];
                        Assert.IsTrue(DbRestrictor.Approved(TmpStation.Code, TmpStation.StationType));
                    }
                }
            }
        }
    }
}
