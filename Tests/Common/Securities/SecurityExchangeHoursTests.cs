﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Securities;
using DayOfWeek = System.DayOfWeek;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityExchangeHoursTests
    {
        [Test]
        public void StartIsOpen()
        {
            var exchangeHours = CreateForexSecurityExchangeHours();

            var date = new DateTime(2015, 6, 21);
            var marketOpen = exchangeHours.MarketHours[DayOfWeek.Sunday].GetMarketOpen(TimeSpan.Zero, false);
            Assert.IsTrue(marketOpen.HasValue);
            var time = (date + marketOpen.Value).AddTicks(-1);
            Assert.IsFalse(exchangeHours.IsOpen(time, false));

            time = time + TimeSpan.FromTicks(1);
            Assert.IsTrue(exchangeHours.IsOpen(time, false));
        }

        [Test]
        public void EndIsClosed()
        {
            var exchangeHours = CreateForexSecurityExchangeHours();

            var date = new DateTime(2015, 6, 19);
            var localMarketHours = exchangeHours.MarketHours[DayOfWeek.Friday];
            var marketClose = localMarketHours.GetMarketClose(TimeSpan.Zero, false);
            Assert.IsTrue(marketClose.HasValue);
            var time = (date + marketClose.Value).AddTicks(-1);
            Assert.IsTrue(exchangeHours.IsOpen(time, false));

            time = time + TimeSpan.FromTicks(1);
            Assert.IsFalse(exchangeHours.IsOpen(time, false));
        }

        [Test]
        public void IntervalOverlappingStartIsOpen()
        {
            var exchangeHours = CreateForexSecurityExchangeHours();

            var date = new DateTime(2015, 6, 21);
            var marketOpen = exchangeHours.MarketHours[DayOfWeek.Sunday].GetMarketOpen(TimeSpan.Zero, false);
            Assert.IsTrue(marketOpen.HasValue);
            var startTime = (date + marketOpen.Value).AddMinutes(-1);

            Assert.IsFalse(exchangeHours.IsOpen(startTime, startTime.AddMinutes(1), false));

            // now the end is 1 tick after open, should return true
            startTime = startTime + TimeSpan.FromTicks(1);
            Assert.IsTrue(exchangeHours.IsOpen(startTime, startTime.AddMinutes(1), false));
        }

        [Test]
        public void IntervalOverlappingEndIsOpen()
        {
            var exchangeHours = CreateForexSecurityExchangeHours();

            var date = new DateTime(2015, 6, 19);
            var marketClose = exchangeHours.MarketHours[DayOfWeek.Friday].GetMarketClose(TimeSpan.Zero, false);
            Assert.IsTrue(marketClose.HasValue);
            var startTime = (date + marketClose.Value).AddMinutes(-1);

            Assert.IsTrue(exchangeHours.IsOpen(startTime, startTime.AddMinutes(1), false));

            // now the start is on the close, returns false
            startTime = startTime.AddMinutes(1);
            Assert.IsFalse(exchangeHours.IsOpen(startTime, startTime.AddMinutes(1), false));
        }

        [Test]
        public void MultiDayInterval()
        {
            var exchangeHours = CreateForexSecurityExchangeHours();

            var date = new DateTime(2015, 6, 19);
            var marketClose = exchangeHours.MarketHours[DayOfWeek.Friday].GetMarketClose(TimeSpan.Zero, false);
            Assert.IsTrue(marketClose.HasValue);
            var startTime = date + marketClose.Value;

            Assert.IsFalse(exchangeHours.IsOpen(startTime, startTime.AddDays(2), false));

            // if we back up one tick it means the bar started at the last moment before market close, this should be included
            Assert.IsTrue(exchangeHours.IsOpen(startTime.AddTicks(-1), startTime.AddDays(2).AddTicks(-1), false));

            // if we advance one tick, it means the bar closed in the first moment after market open
            Assert.IsTrue(exchangeHours.IsOpen(startTime.AddTicks(1), startTime.AddDays(2).AddTicks(1), false));
        }

        [Test]
        public void MarketIsOpenBeforeEarlyClose()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var localDateTime = new DateTime(2016, 11, 25, 12, 0, 0);
            Assert.IsTrue(exchangeHours.IsOpen(localDateTime, false));
        }

        [Test]
        public void MarketIsNotOpenAfterEarlyClose()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var localDateTime = new DateTime(2016, 11, 25, 14, 0, 0);
            Assert.IsFalse(exchangeHours.IsOpen(localDateTime, false));
        }

        [Test]
        public void MarketIsNotOpenForIntervalAfterEarlyClose()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var startLocalDateTime = new DateTime(2016, 11, 25, 13, 0, 0);
            var endLocalDateTime = new DateTime(2016, 11, 25, 13, 30, 0);
            Assert.IsFalse(exchangeHours.IsOpen(startLocalDateTime, endLocalDateTime, false));
        }

        [Test]
        public void GetNextMarketOpenIsNonInclusiveOfStartTime()
        {
            var exhangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2015, 6, 30, 9, 30, 0);
            var nextMarketOpen = exhangeHours.GetNextMarketOpen(startTime, false);
            Assert.AreEqual(startTime.AddDays(1), nextMarketOpen);
        }

        [Test]
        public void GetNextMarketOpenWorksOnHoliday()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2016, 9, 5, 8, 0, 0);
            var nextMarketOpen = exchangeHours.GetNextMarketOpen(startTime, false);
            Assert.AreEqual(new DateTime(2016, 9, 6, 9, 30, 0), nextMarketOpen);
        }

        [Test]
        public void GetNextMarketOpenWorksOverWeekends()
        {
            var exhangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2015, 6, 26, 9, 30, 1);
            var nextMarketOpen = exhangeHours.GetNextMarketOpen(startTime, false);
            Assert.AreEqual(new DateTime(2015, 6, 29, 9, 30, 0), nextMarketOpen);
        }

        [Test]
        public void GetNextMarketCloseIsNonInclusiveOfStartTime()
        {
            var exhangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2015, 6, 30, 16, 0, 0);
            var nextMarketOpen = exhangeHours.GetNextMarketClose(startTime, false);
            Assert.AreEqual(startTime.AddDays(1), nextMarketOpen);
        }

        [Test]
        public void GetNextMarketCloseWorksOnHoliday()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2016, 9, 5, 10, 0, 0);
            var nextMarketClose = exchangeHours.GetNextMarketClose(startTime, false);
            Assert.AreEqual(new DateTime(2016, 9, 6, 16, 0, 0), nextMarketClose);
        }

        [Test]
        public void GetNextMarketCloseWorksOverWeekends()
        {
            var exhangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2015, 6, 26, 16, 0, 1);
            var nextMarketClose = exhangeHours.GetNextMarketClose(startTime, false);
            Assert.AreEqual(new DateTime(2015, 6, 29, 16, 0, 0), nextMarketClose);
        }

        [Test]
        public void GetNextMarketCloseWorksBeforeEarlyClose()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2016, 11, 25, 10, 0, 0);
            var nextMarketClose = exchangeHours.GetNextMarketClose(startTime, false);
            Assert.AreEqual(new DateTime(2016, 11, 25, 13, 0, 0), nextMarketClose);
        }

        [Test]
        public void GetNextMarketCloseWorksAfterEarlyClose()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2016, 11, 25, 14, 0, 0);
            var nextMarketClose = exchangeHours.GetNextMarketClose(startTime, false);
            Assert.AreEqual(new DateTime(2016, 11, 28, 16, 0, 0), nextMarketClose);
        }

        [Test]
        public void Benchmark()
        {
            var forex = CreateForexSecurityExchangeHours();

            var reference = new DateTime(1991, 06, 20);
            forex.IsOpen(reference, false);
            forex.IsOpen(reference, reference.AddDays(1), false);

            const int length = 1000*1000*1;

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < length; i++)
            {
                forex.IsOpen(reference.AddMinutes(1), false);
            }
            stopwatch.Stop();

            Console.WriteLine("forex1: " + stopwatch.Elapsed);
        }

        [Test]
        public void RegularMarketDurationIsFromMostCommonLocalMarketHours()
        {
            var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, Enumerable.Empty<DateTime>(),
                new Dictionary<DayOfWeek, LocalMarketHours>
                {
                    // fake market hours schedule with random durations, the most common of which is 5 hours and 2 hours, it will pick the larger
                    {DayOfWeek.Sunday, new LocalMarketHours(DayOfWeek.Sunday, TimeSpan.FromHours(4), TimeSpan.FromHours(6))},           //2hr
                    {DayOfWeek.Monday, new LocalMarketHours(DayOfWeek.Monday, TimeSpan.FromHours(13), TimeSpan.FromHours(15))},         //2hr
                    {DayOfWeek.Tuesday, new LocalMarketHours(DayOfWeek.Tuesday, TimeSpan.FromHours(5), TimeSpan.FromHours(10))},        //5hr
                    {DayOfWeek.Wednesday, new LocalMarketHours(DayOfWeek.Wednesday, TimeSpan.FromHours(5), TimeSpan.FromHours(10))},    //5hr
                    {DayOfWeek.Thursday, new LocalMarketHours(DayOfWeek.Thursday, TimeSpan.FromHours(1), TimeSpan.FromHours(23))},      //22hr
                    {DayOfWeek.Friday, new LocalMarketHours(DayOfWeek.Friday, TimeSpan.FromHours(0), TimeSpan.FromHours(23))},          //23hr
                    {DayOfWeek.Saturday, new LocalMarketHours(DayOfWeek.Saturday, TimeSpan.FromHours(3), TimeSpan.FromHours(23))},      //20hr
                }, new Dictionary<DateTime, TimeSpan>());

            Assert.AreEqual(TimeSpan.FromHours(5), exchangeHours.RegularMarketDuration);
        }

        public static SecurityExchangeHours CreateForexSecurityExchangeHours()
        {
            var sunday = new LocalMarketHours(DayOfWeek.Sunday, new TimeSpan(17, 0, 0), TimeSpan.FromTicks(Time.OneDay.Ticks - 1));
            var monday = LocalMarketHours.OpenAllDay(DayOfWeek.Monday);
            var tuesday = LocalMarketHours.OpenAllDay(DayOfWeek.Tuesday);
            var wednesday = LocalMarketHours.OpenAllDay(DayOfWeek.Wednesday);
            var thursday = LocalMarketHours.OpenAllDay(DayOfWeek.Thursday);
            var friday = new LocalMarketHours(DayOfWeek.Friday, TimeSpan.Zero, new TimeSpan(17, 0, 0));
            var saturday = LocalMarketHours.ClosedAllDay(DayOfWeek.Saturday);

            var earlyCloses = new Dictionary<DateTime, TimeSpan>();
            var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, USHoliday.Dates.Select(x => x.Date), new[]
            {
                sunday, monday, tuesday, wednesday, thursday, friday//, saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses);
            return exchangeHours;
        }

        public static SecurityExchangeHours CreateUsEquitySecurityExchangeHours()
        {
            var sunday = LocalMarketHours.ClosedAllDay(DayOfWeek.Sunday);
            var monday = new LocalMarketHours(DayOfWeek.Monday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var tuesday = new LocalMarketHours(DayOfWeek.Tuesday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var wednesday = new LocalMarketHours(DayOfWeek.Wednesday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var thursday = new LocalMarketHours(DayOfWeek.Thursday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var friday = new LocalMarketHours(DayOfWeek.Friday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var saturday = LocalMarketHours.ClosedAllDay(DayOfWeek.Saturday);

            var earlyCloses = new Dictionary<DateTime, TimeSpan> { { new DateTime(2016, 11, 25), new TimeSpan(13, 0, 0) } };
            var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, USHoliday.Dates.Select(x => x.Date), new[]
            {
                sunday, monday, tuesday, wednesday, thursday, friday, saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses);
            return exchangeHours;
        }
    }
}
