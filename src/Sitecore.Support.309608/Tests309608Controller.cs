using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using Sitecore.ContentTesting;
using Sitecore.ContentTesting.ContentSearch.Models;
using Sitecore.ContentTesting.Data;
using Sitecore.ContentTesting.Extensions;
using Sitecore.ContentTesting.Helpers;
using Sitecore.ContentTesting.Model;
using Sitecore.ContentTesting.Model.Data.Items;
using Sitecore.ContentTesting.Models;
using Sitecore.ContentTesting.Reports;
using Sitecore.ContentTesting.Requests.Controllers;
using Sitecore.ContentTesting.ViewModel;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;

namespace Sitecore.Support.ContentTesting.Requests.Controllers.Optimization
{
    public class Tests309608Controller : ContentTestingControllerBase
    {
        private const string DateFormat = "dd-MMM-yyyy";

        private readonly IContentTestStore _contentTestStore;

        public Tests309608Controller()
            : this(ContentTestingFactory.Instance.ContentTestStore)
        {
        }

        public Tests309608Controller(IContentTestStore contentTestStore)
        {
            _contentTestStore = contentTestStore;
        }

        [HttpGet]
        public IHttpActionResult GetDraftPageTests(int? page = default(int?), int? pageSize = default(int?), string hostItemId = null, string searchText = null)
        {
            page = (page ?? 1);
            pageSize = (pageSize ?? 20);
            DataUri hostItemDataUri = null;
            if (!string.IsNullOrEmpty(hostItemId))
            {
                hostItemDataUri = ParseDataUri(hostItemId);
            }
            IEnumerable<TestingSearchResultItem> draftTests = base.ContentTestStore.GetDraftTests(hostItemDataUri, searchText);
            draftTests = from x in draftTests
                         orderby x.UpdatedDate
                         select x;
            List<TestingSearchResultItem> list = new List<TestingSearchResultItem>();
            Dictionary<ItemUri, TestDefinitionItem> dictionary = new Dictionary<ItemUri, TestDefinitionItem>();
            foreach (TestingSearchResultItem item4 in draftTests)
            {
                Item item = Database.GetItem(item4.Uri);
                if (item != null && !(item4.HostItemUri == null))
                {
                    TestDefinitionItem testDefinitionItem = TestDefinitionItem.Create(item);
                    if (testDefinitionItem != null && testDefinitionItem.PageLevelTestVariables.Count == testDefinitionItem.Variables.Count)
                    {
                        Item item2 = testDefinitionItem.Database.GetItem(item4.HostItemUri);
                        if (item2 != null && testDefinitionItem.InnerItem[FieldIDs.CreatedBy].Equals(Context.User.Name))
                        {
                            list.Add(item4);
                            dictionary.Add(item4.Uri, testDefinitionItem);
                        }
                    }
                }
            }
            List<TestingSearchResultItem> list2 = list.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value).ToList();
            List<DraftTestViewModel> list3 = new List<DraftTestViewModel>();
            foreach (TestingSearchResultItem item5 in list2)
            {
                string username = item5.FriendlyOwner;
                TestDefinitionItem testDefinitionItem2 = dictionary[item5.Uri];
                if (testDefinitionItem2 != null)
                {
                    username = testDefinitionItem2.InnerItem[FieldIDs.CreatedBy];
                }
                DraftTestViewModel draftTestViewModel = new DraftTestViewModel
                {
                    CreatedBy = FormattingHelper.GetFriendlyUserName(username),
                    Date = DateUtil.ToServerTime(item5.CreatedDate).ToString("dd-MMM-yyyy"),
                    SaveDate = DateUtil.ToServerTime(item5.UpdatedDate).ToString("dd-MMM-yyyy"),
                    ItemId = dictionary[item5.Uri].ID.ToString()
                };
                if (item5.HostItemUri != null)
                {
                    Item item3 = testDefinitionItem2.Database.GetItem(item5.HostItemUri);
                    if (item3 == null)
                    {
                        continue;
                    }
                    TestSet testSet = TestManager.GetTestSet(new TestDefinitionItem[1]
                    {
                    testDefinitionItem2
                    }, item3, Context.Device.ID);
                    draftTestViewModel.ExperienceCount = testSet.GetExperienceCount();
                    draftTestViewModel.HostPageId = item3.ID.ToString();
                    draftTestViewModel.HostPageUri = item3.Uri.ToDataUri();
                    draftTestViewModel.HostPageName = item3.DisplayName;
                    draftTestViewModel.Language = item3.Language.Name;
                }
                list3.Add(draftTestViewModel);
            }
            return Json(new TestListViewModel
            {
                Items = list3,
                TotalResults = list.Count()
            });
        }

        [HttpGet]
        public JsonResult<TestListViewModel> GetActiveTests(int? page = default(int?), int? pageSize = default(int?), string hostItemId = null, string searchText = null)
        {
            page = (page ?? 1);
            pageSize = (pageSize ?? 20);
            DataUri hostItemDataUri = null;
            if (!string.IsNullOrEmpty(hostItemId))
            {
                hostItemDataUri = DataUriParser.Parse(hostItemId);
            }
            TestingSearchResultItem[] array = base.ContentTestStore.GetActiveTests(hostItemDataUri, searchText).ToArray();
            List<ExecutedTestViewModel> list = new List<ExecutedTestViewModel>();
            Dictionary<ID, ITestConfiguration> dictionary = new Dictionary<ID, ITestConfiguration>();
            TestingSearchResultItem[] array2 = array;
            foreach (TestingSearchResultItem testingSearchResultItem in array2)
            {
                Item item = Database.GetItem(testingSearchResultItem.Uri);
                if (item == null)
                {
                    continue;
                }
                TestDefinitionItem testDefinitionItem = TestDefinitionItem.Create(item);
                if (testDefinitionItem == null)
                {
                    continue;
                }
                Item item2 = (testingSearchResultItem.HostItemUri != null) ? item.Database.GetItem(testingSearchResultItem.HostItemUri) : null;
                if (item2 != null)
                {
                    ITestConfiguration testConfiguration = _contentTestStore.LoadTestForItem(item2, testDefinitionItem);
                    if (testConfiguration != null)
                    {
                        dictionary.Add(testConfiguration.TestDefinitionItem.ID, testConfiguration);
                        list.Add(new ExecutedTestViewModel
                        {
                            HostPageId = item2.ID.ToString(),
                            HostPageUri = item2.Uri.ToDataUri(),
                            HostPageName = item2.DisplayName,
                            DeviceId = testConfiguration.DeviceId.ToString(),
                            DeviceName = testConfiguration.DeviceName,
                            Language = testConfiguration.LanguageName,
                            CreatedBy = FormattingHelper.GetFriendlyUserName(item.Security.GetOwner()),
                            Date = DateUtil.ToServerTime(testDefinitionItem.StartDate).ToString("dd-MMM-yyyy"),
                            ExperienceCount = testConfiguration.TestSet.GetExperienceCount(),
                            Days = GetEstimatedDurationDays(item2, testConfiguration.TestSet.GetExperienceCount(), testDefinitionItem),
                            ItemId = testDefinitionItem.ID.ToString(),
                            ContentOnly = (testConfiguration.TestSet.Variables.Count == testDefinitionItem.PageLevelTestVariables.Count),
                            TestType = testConfiguration.TestType,
                            TestId = testConfiguration.TestDefinitionItem.ID
                        });
                    }
                }
            }
            list = (from x in list
                    orderby x.Days
                    select x).Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value).ToList();
            foreach (ExecutedTestViewModel item4 in list)
            {
                Item item3 = base.Database.GetItem(item4.HostPageUri);
                if (item3 != null)
                {
                    item4.Effect = GetWinningEffect(dictionary[item4.TestId]);
                    if (item4.Effect < 0.0)
                    {
                        item4.EffectCss = "value-decrease";
                    }
                    else if (item4.Effect == 0.0)
                    {
                        item4.EffectCss = "value-nochange";
                    }
                    else
                    {
                        item4.EffectCss = "value-increase";
                    }
                }
            }
            return Json(new TestListViewModel
            {
                Items = list,
                TotalResults = array.Count()
            });
        }

        [HttpGet]
        public IHttpActionResult GetHistoricalTests(int? page = default(int?), int? pageSize = default(int?), string hostItemId = null, string searchText = null, string language = null)
        {
            page = (page ?? 1);
            pageSize = (pageSize ?? 20);
            DataUri hostItemDataUri = null;
            ID result = null;
            if (!string.IsNullOrEmpty(hostItemId))
            {
                ID.TryParse(hostItemId, out result);
            }
            Language result2 = Context.Language;
            if (!string.IsNullOrEmpty(language))
            {
                Language.TryParse(language, out result2);
            }
            if (result != (ID)null || !string.IsNullOrEmpty(language))
            {
                hostItemDataUri = new DataUri(result, result2);
            }
            IEnumerable<TestingSearchResultItem> historicalTests = base.ContentTestStore.GetHistoricalTests(hostItemDataUri, searchText);
            IEnumerable<TestingSearchResultItem> enumerable = (from x in historicalTests
                                                               orderby x.UpdatedDate
                                                               select x).Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
            List<ExecutedTestViewModel> list = new List<ExecutedTestViewModel>();
            foreach (TestingSearchResultItem item3 in enumerable)
            {
                Item item = Database.GetItem(item3.Uri);
                if (item != null)
                {
                    TestDefinitionItem testDefinitionItem = TestDefinitionItem.Create(item);
                    if (testDefinitionItem != null && !(item3.HostItemUri == null))
                    {
                        Item item2 = item.Database.GetItem(item3.HostItemUri);
                        if (item2 != null)
                        {
                            TestConfiguration testConfiguration = new TestConfiguration(item2, testDefinitionItem.Device.TargetID, testDefinitionItem);
                            ID iD = testConfiguration.DeviceId;
                            if (testConfiguration.DeviceId == ItemIDs.Null)
                            {
                                iD = item2.Database.Resources.Devices.GetAll().FirstOrDefault((DeviceItem d) => d.Name.ToLower() == "default")?.ID;
                            }
                            ExecutedTestViewModel executedTestViewModel = new ExecutedTestViewModel
                            {
                                HostPageId = item2.ID.ToString(),
                                HostPageUri = item2.Uri.ToDataUri(),
                                HostPageName = item2.DisplayName,
                                DeviceId = ((testConfiguration.DeviceId == ID.Null) ? iD.ToString() : testConfiguration.DeviceId.ToString()),
                                DeviceName = testConfiguration.DeviceName,
                                Language = testConfiguration.LanguageName,
                                CreatedBy = FormattingHelper.GetFriendlyUserName(item.Security.GetOwner()),
                                ItemId = testDefinitionItem.ID.ToString(),
                                ContentOnly = (testDefinitionItem.Variables.Count == testDefinitionItem.PageLevelTestVariables.Count)
                            };
                            HistoricalDataModel historicalTestData = testDefinitionItem.GetHistoricalTestData();
                            if (historicalTestData != null)
                            {
                                executedTestViewModel.Date = (historicalTestData.IsTestCanceled ? (historicalTestData.EndDate + string.Format(" ({0})", Translate.Text("Test was cancelled"))) : historicalTestData.EndDate);
                                executedTestViewModel.ExperienceCount = historicalTestData.ExperiencesCount;
                                executedTestViewModel.Days = historicalTestData.TestDuration;
                                executedTestViewModel.Effect = historicalTestData.Effect;
                                executedTestViewModel.TestScore = historicalTestData.TestScore;
                            }
                            else
                            {
                                executedTestViewModel.Date = "--";
                                executedTestViewModel.ExperienceCount = testConfiguration.TestSet.GetExperiences().Count();
                            }
                            if (executedTestViewModel.Effect < 0.0)
                            {
                                executedTestViewModel.EffectCss = "value-decrease";
                            }
                            else if (executedTestViewModel.Effect > 0.0)
                            {
                                executedTestViewModel.EffectCss = "value-increase";
                            }
                            else
                            {
                                executedTestViewModel.EffectCss = "value-nochange";
                            }
                            list.Add(executedTestViewModel);
                        }
                    }
                }
            }
            return Json(new TestListViewModel
            {
                Items = list,
                TotalResults = historicalTests.Count()
            });
        }

        [HttpGet]
        public JsonResult<TestListViewModel> GetSuggestedTests(int? page = default(int?), int? pageSize = default(int?), string hostItemId = null, string searchText = null)
        {
            page = (page ?? 1);
            pageSize = (pageSize ?? 20);
            DataUri hostItemDataUri = null;
            if (!string.IsNullOrEmpty(hostItemId))
            {
                hostItemDataUri = DataUriParser.Parse(hostItemId);
            }
            SuggestedTestSearchResultItem[] array = base.ContentTestStore.GetSuggestedTests(hostItemDataUri, searchText).ToArray();
            List<SuggestedTestViewModel> list = new List<SuggestedTestViewModel>();
            int num = (page.Value - 1) * pageSize.Value;
            while (list.Count < pageSize && num < array.Length)
            {
                SuggestedTestSearchResultItem suggestedTestSearchResultItem = array[num];
                Item item = base.Database.GetItem(suggestedTestSearchResultItem.ItemId);
                if (item != null)
                {
                    list.Add(new SuggestedTestViewModel
                    {
                        HostPageId = item.ID.ToString(),
                        HostPageUri = item.Uri.ToDataUri(),
                        HostPageName = item.DisplayName,
                        Language = item.Language.Name,
                        Impact = suggestedTestSearchResultItem.Impact,
                        Potential = suggestedTestSearchResultItem.Potential,
                        Recommendation = suggestedTestSearchResultItem.Recommendation
                    });
                }
                num++;
            }
            return Json(new TestListViewModel
            {
                Items = list,
                TotalResults = list.Count()
            });
        }

        private static int GetEstimatedDurationDays(Item hostItem, int experienceCount, TestDefinitionItem testDef)
        {
            string deviceName = string.Empty;
            if (testDef.Device.TargetItem != null)
            {
                deviceName = testDef.Device.TargetItem.Name;
            }
            TestRunEstimator testRunEstimator = ContentTestingFactory.Instance.GetTestRunEstimator(testDef.Language, deviceName);
            testRunEstimator.HostItem = hostItem;
            TestRunEstimate estimate = testRunEstimator.GetEstimate(experienceCount, 0.8, testDef.TrafficAllocationPercentage, testDef.ConfidenceLevelPercentage, testDef);
            DateTime d = testDef.StartDate.AddDays(estimate.EstimatedDayCount.HasValue ? ((double)estimate.EstimatedDayCount.Value) : 0.0);
            int num = (int)Math.Ceiling((d - DateTime.UtcNow).TotalDays);
            TimeSpan timeSpan = DateTime.UtcNow - testDef.StartDate;
            int num2 = int.Parse(testDef.MaxDuration);
            if (num <= 0)
            {
                num = num2;
            }
            num = Math.Min(num, num2 - timeSpan.Days);
            num = Math.Max(num, int.Parse(testDef.MinDuration) - timeSpan.Days);
            return Math.Max(num, 0);
        }

        private double GetWinningEffect(ITestConfiguration test)
        {
            IContentTestPerformance performanceForTest = base.PerformanceFactory.GetPerformanceForTest(test);
            if (performanceForTest.BestExperiencePerformance != null)
            {
                return performanceForTest.GetExperienceEffect(performanceForTest.BestExperiencePerformance.Combination);
            }
            return 0.0;
        }
    }
}