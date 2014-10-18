﻿using System;
using System.IO;
using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LzTimerTests
{
    public abstract class PeriodStoreageTests
    {
        private DateTime START_DATETIME = new DateTime(2014, 1, 1, 12, 0, 0);

        public void howTreatIdenticalPeriods()
        {
            var activePeriod = PeriodBuilder.New(START_DATETIME).Active();
            var idlePeriod = PeriodBuilder.New(START_DATETIME).Idle();
            // todo:
        }

        [TestMethod]
        public void addedPeriod_shouldBeReaderByAnotherInstance()
        {
            var activePeriod = PeriodBuilder.New(START_DATETIME).Active();
            var idlePeriod = PeriodBuilder.NewAfter(activePeriod, 1.s()).Idle();
            var expected = new Period[] { activePeriod, idlePeriod };
            {
                PeriodStorage instance1 = GetStorage();
                instance1.Add(activePeriod);
                instance1.Add(idlePeriod);

                CollectionAssert.AreEquivalent(expected, instance1.GetAll());
                instance1.Close();
            }

            if (IsPersisent())
            {
                WaitForConnectionToDbClosed();
                PeriodStorage instance2 = GetStorage();
                CollectionAssert.AreEquivalent(expected, instance2.GetAll());
                instance2.Close();
            }
        }

        [TestMethod]
        public void removePeriod_shouldWork()
        {
            var firstPeriod = PeriodBuilder.New(START_DATETIME).Length(5.s()).Active();
            var secondPeriod = PeriodBuilder.NewAfter(firstPeriod, 10.s()).Idle();

            PeriodStorage periodStorageSUT = GetStorage();
            periodStorageSUT.Add(firstPeriod);
            periodStorageSUT.Add(secondPeriod);
            var expected = new Period[] { secondPeriod };            
            
            periodStorageSUT.Remove(firstPeriod);
            CollectionAssert.AreEquivalent(expected, periodStorageSUT.GetAll());
            periodStorageSUT.Close();

            if (IsPersisent())
            {
                WaitForConnectionToDbClosed();
                PeriodStorage newInstance = GetStorage();
                CollectionAssert.AreEquivalent(expected, newInstance.GetAll());
                newInstance.Close();
            }
        }

        [TestMethod]
        public void getPeriodsFromTimePeriod_shouldWork()
        {
            var firstPeriod = PeriodBuilder.New(START_DATETIME).Length(5.s()).Active();
            var secondPeriod = PeriodBuilder.NewAfter(firstPeriod, 10.s()).Idle();

            PeriodStorage periodStorageSUT = GetStorage();
            periodStorageSUT.Add(firstPeriod);
            periodStorageSUT.Add(secondPeriod);
            var expected = new Period[] { firstPeriod };

            var found = periodStorageSUT.GetPeriodsFromTimePeriod(
                new TimePeriod(
                    firstPeriod.Start - 1.s(),
                    firstPeriod.End + 1.s()));
            CollectionAssert.AreEquivalent(expected, found);
            periodStorageSUT.Close();
        }

        [TestMethod]
        public void getPeriodsAfter_shouldWork()
        {
            var firstPeriod = PeriodBuilder.New(START_DATETIME).Length(5.s()).Active();
            var secondPeriod = PeriodBuilder.NewAfter(firstPeriod, 10.s()).Idle();

            PeriodStorage periodStorageSUT = GetStorage();
            periodStorageSUT.Add(firstPeriod);
            periodStorageSUT.Add(secondPeriod);
            var expected = new Period[] { secondPeriod };

            var found = periodStorageSUT.GetPeriodsAfter(firstPeriod.End + 1.s());
            CollectionAssert.AreEquivalent(expected, found);
            periodStorageSUT.Close();
        }

        [TestMethod]
        public void GetSinceFirstActivePeriodBeforeTest()
        {
            var firstPeriod  = START_DATETIME.Length(5.s()).Active();
            var secondPeriod = firstPeriod.NewPeriodAfter(10.s()).Idle();

            PeriodStorage periodStorageSUT = GetStorage();
            periodStorageSUT.Add(firstPeriod);
            periodStorageSUT.Add(secondPeriod);
            var expected = new Period[] { secondPeriod };

            var found = periodStorageSUT.GetPeriodsAfter(firstPeriod.End + 1.s());
            CollectionAssert.AreEquivalent(expected, found);
            periodStorageSUT.Close();
        }

        protected abstract PeriodStorage GetStorage();

        protected abstract bool IsPersisent();

        protected static void WaitForConnectionToDbClosed()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    [TestClass]
    public class SqliteStoragePeriodTests : PeriodStoreageTests
    {
        private const string DB_FILE = "test.db";

        [TestInitializeAttribute]
        public void setUp()
        {
            WaitForConnectionToDbClosed();
            File.Delete(DB_FILE);
        }

        protected override PeriodStorage GetStorage()
        {
            return new SqlitePeriodStorage(DB_FILE);
        }

        protected override bool IsPersisent()
        {
            return true;
        }
    }

    [TestClass]
    public class MemoryStoragePeriodTests : PeriodStoreageTests
    {
        [TestInitializeAttribute]
        public void setUp()
        {
        }

        protected override PeriodStorage GetStorage()
        {
            return new MemoryPeriodStorage();
        }

        protected override bool IsPersisent()
        {
            return false;
        }
    }
}
