﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine.U2D;

namespace AddressableAssetsIntegrationTests
{
    internal abstract partial class AddressablesIntegrationTests : IPrebuildSetup
    {
        [UnityTest]
        public IEnumerator AsyncCache_IsCleaned_OnFailedOperation()
        {
            yield return Init();

            var op = m_Addressables.LoadAssetAsync<GameObject>("notARealKey");
            op.Completed += handle =>
            {
                Assert.AreEqual(0, m_Addressables.ResourceManager.CachedOperationCount());
            };

            yield return op;
        }

        [UnityTest]
        public IEnumerator LoadResourceLocations_InvalidKeyDoesNotThrow()
        {
            //Setup
            yield return Init();
            
            //Test
            Assert.DoesNotThrow( () =>
            {
                m_Addressables.LoadResourceLocationsAsync("noSuchLabel", typeof(object));
            });
        }
        
        [UnityTest]
        public IEnumerator LoadResourceLocations_ValidKeyDoesNotThrow()
        {
            //Setup
            yield return Init();
            
            //Test
            Assert.DoesNotThrow( () =>
            {
                m_Addressables.LoadResourceLocationsAsync(AddressablesTestUtility.GetPrefabLabel("BASE"), typeof(GameObject));
            });
        }

        [UnityTest]
        public IEnumerator LoadAsset_InvalidKeyThrowsInvalidKeyException()
        {
            //Setup
            yield return Init();
            
            //Test
            AsyncOperationHandle handle = default(AsyncOperationHandle);
            handle = m_Addressables.LoadAssetAsync<GameObject>("noSuchLabel");
            Assert.AreEqual("Exception of type 'UnityEngine.AddressableAssets.InvalidKeyException' was thrown., Key=noSuchLabel", handle.OperationException.Message);
            yield return handle;
            
            //Cleanup
            handle.Release();
        }

        [UnityTest]
        public IEnumerator CanLoadTextureAsSprite()
        {
            //Setup
            yield return Init();

            var op = m_Addressables.LoadAssetAsync<Sprite>("sprite");
            yield return op;
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(typeof(Sprite), op.Result.GetType());
            op.Release();
        }

        [UnityTest]
        public IEnumerator CanLoadSpriteByName()
        {
            //Setup
            yield return Init();

            var op = m_Addressables.LoadAssetAsync<Sprite>("sprite[botright]");
            yield return op;
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(typeof(Sprite), op.Result.GetType());
            Assert.AreEqual("botright", op.Result.name);
            op.Release();

            var op2 = m_Addressables.LoadAssetAsync<Sprite>("sprite[topleft]");
            yield return op2;
            Assert.IsNotNull(op2.Result);
            Assert.AreEqual(typeof(Sprite), op2.Result.GetType());
            Assert.AreEqual("topleft", op2.Result.name);
            op2.Release();
        }

        [UnityTest]
        public IEnumerator CanLoadAllSpritesAsArray()
        {
            //Setup
            yield return Init();
            
            var op = m_Addressables.LoadAssetAsync<Sprite[]>("sprite");
            yield return op;
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(typeof(Sprite[]), op.Result.GetType());
            Assert.AreEqual(2, op.Result.Length);
            op.Release();
        }

        [UnityTest]
        public IEnumerator CanLoadAllSpritesAsList()
        {
            //Setup
            yield return Init();

            var op = m_Addressables.LoadAssetAsync<IList<Sprite>>("sprite");
            yield return op;
            Assert.IsNotNull(op.Result);
            Assert.IsTrue(typeof(IList<Sprite>).IsAssignableFrom(op.Result.GetType()));
            Assert.AreEqual(2, op.Result.Count);
            op.Release();
        }


        [UnityTest]
        public IEnumerator CanLoadTextureAsTexture()
        {
            //Setup
            yield return Init();

            var op = m_Addressables.LoadAssetAsync<Texture2D>("sprite");
            yield return op;
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(typeof(Texture2D), op.Result.GetType());
            op.Release();
        }

        [UnityTest]
        public IEnumerator LoadAsset_ValidKeyDoesNotThrow()
        {
            //Setup
            yield return Init();
            
            //Test
            AsyncOperationHandle handle = default(AsyncOperationHandle);
            Assert.DoesNotThrow( () =>
            {
                handle = m_Addressables.LoadAssetAsync<GameObject>(AddressablesTestUtility.GetPrefabLabel("BASE"));
            });
            yield return handle;
            
            //Cleanup
            handle.Release();
        }

        [UnityTest]
        public IEnumerator VerifyChainOpPercentCompleteCalculation()
        {
            //Setup
            yield return Init();
            AsyncOperationHandle<GameObject> op = m_Addressables.LoadAssetAsync<GameObject>(AddressablesTestUtility.GetPrefabLabel("BASE"));

            //Test            
            while (op.PercentComplete < 1)
            {
                Assert.False(op.IsDone);
                yield return null;
            }
            Assert.True(op.PercentComplete == 1 && op.IsDone);
            yield return op;

            //Cleanup
            op.Release();
        }
            
        [UnityTest]
        public IEnumerator VerifyProfileVariableEvaluation()
        {
            yield return Init();
            Assert.AreEqual(string.Format("{0}", m_Addressables.RuntimePath), AddressablesRuntimeProperties.EvaluateString("{UnityEngine.AddressableAssets.Addressables.RuntimePath}"));
        }

        [UnityTest]
        public IEnumerator VerifyDownloadSize()
        {
            yield return Init();
            long expectedSize = 0;
            var locMap = new ResourceLocationMap("TestLocator");

            var bundleLoc1 = new ResourceLocationBase("sizeTestBundle1", "http://nowhere.com/mybundle1.bundle", typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData1 = (bundleLoc1.Data = CreateLocationSizeData("sizeTestBundle1", 1000, 123, "hashstring1")) as ILocationSizeData;
            if (sizeData1 != null)
                expectedSize += sizeData1.ComputeSize(bundleLoc1);

            var bundleLoc2 = new ResourceLocationBase("sizeTestBundle2", "http://nowhere.com/mybundle2.bundle", typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData2 = (bundleLoc2.Data = CreateLocationSizeData("sizeTestBundle2", 500, 123, "hashstring2")) as ILocationSizeData;
            if (sizeData2 != null)
                expectedSize += sizeData2.ComputeSize(bundleLoc2);

            var assetLoc = new ResourceLocationBase("sizeTestAsset", "myASset.asset", typeof(BundledAssetProvider).FullName, typeof(object), bundleLoc1, bundleLoc2);

            locMap.Add("sizeTestBundle1", bundleLoc1);
            locMap.Add("sizeTestBundle2", bundleLoc2);
            locMap.Add("sizeTestAsset", assetLoc);
            m_Addressables.AddResourceLocator(locMap);

            var dOp = m_Addressables.GetDownloadSizeAsync("sizeTestAsset");
            yield return dOp;
            Assert.AreEqual(expectedSize, dOp.Result);
            dOp.Release();
        }

        [UnityTest]
        public IEnumerator GetDownloadSize_CalculatesCachedBundles()
        {
#if ENABLE_CACHING
            yield return Init();
            long expectedSize = 0;
            long bundleSize1 = 1000;
            long bundleSize2 = 500;
            var locMap = new ResourceLocationMap("TestLocator");

            Caching.ClearCache();
            //Simulating a cached bundle
            string fakeCachePath = CreateFakeCachedBundle("GetDownloadSize_CalculatesCachedBundlesBundle1", "be38e35d2177c282d5d6a2e54a803aab");

            var bundleLoc1 = new ResourceLocationBase("sizeTestBundle1", "http://nowhere.com/GetDownloadSize_CalculatesCachedBundlesBundle1.bundle",
                typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData1 =
                (bundleLoc1.Data = CreateLocationSizeData("cachedSizeTestBundle1", bundleSize1, 123,
                    "be38e35d2177c282d5d6a2e54a803aab")) as ILocationSizeData;
            if (sizeData1 != null)
                expectedSize += sizeData1.ComputeSize(bundleLoc1);

            var bundleLoc2 = new ResourceLocationBase("cachedSizeTestBundle2", "http://nowhere.com/GetDownloadSize_CalculatesCachedBundlesBundle2.bundle",
                typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData2 =
                (bundleLoc2.Data = CreateLocationSizeData("cachedSizeTestBundle2", bundleSize2, 123,
                    "d9fe965a6b253fb9dbd3e1cb08b7d66f")) as ILocationSizeData;
            if (sizeData2 != null)
                expectedSize += sizeData2.ComputeSize(bundleLoc2);

            var assetLoc = new ResourceLocationBase("cachedSizeTestAsset", "myASset.asset",
                typeof(BundledAssetProvider).FullName, typeof(object), bundleLoc1, bundleLoc2);

            locMap.Add("cachedSizeTestBundle1", bundleLoc1);
            locMap.Add("cachedSizeTestBundle2", bundleLoc2);
            locMap.Add("cachedSizeTestAsset", assetLoc);
            m_Addressables.AddResourceLocator(locMap);

            var dOp = m_Addressables.GetDownloadSizeAsync("cachedSizeTestAsset");
            yield return dOp;
            Assert.IsTrue((bundleSize1 + bundleSize2) >  dOp.Result);
            Assert.AreEqual(expectedSize, dOp.Result);

            dOp.Release();
            m_Addressables.RemoveResourceLocator(locMap);
            Directory.Delete(fakeCachePath, true);
#else
            Assert.Ignore();
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator GetDownloadSize_WithList_CalculatesCachedBundles()
        {
#if ENABLE_CACHING
            yield return Init();
            long expectedSize = 0;
            long bundleSize1 = 1000;
            long bundleSize2 = 500;
            var locMap = new ResourceLocationMap("TestLocator");

            Assert.IsTrue(Caching.ClearCache(), "Was unable to clear the cache.  Test results are affected");
            //Simulating a cached bundle
            string fakeCachePath = CreateFakeCachedBundle("GetDownloadSize_WithList_CalculatesCachedBundlesBundle1", "0e38e35d2177c282d5d6a2e54a803aab");

            var bundleLoc1 = new ResourceLocationBase("sizeTestBundle1", "http://nowhere.com/GetDownloadSize_WithList_CalculatesCachedBundlesBundle1.bundle",
                typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData1 =
                (bundleLoc1.Data = CreateLocationSizeData("cachedSizeTestBundle1", bundleSize1, 123,
                    "0e38e35d2177c282d5d6a2e54a803aab")) as ILocationSizeData;
            if (sizeData1 != null)
                expectedSize += sizeData1.ComputeSize(bundleLoc1);

            var bundleLoc2 = new ResourceLocationBase("cachedSizeTestBundle2", "http://nowhere.com/GetDownloadSize_WithList_CalculatesCachedBundlesBundle2.bundle",
                typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData2 =
                (bundleLoc2.Data = CreateLocationSizeData("cachedSizeTestBundle2", bundleSize2, 123,
                    "09fe965a6b253fb9dbd3e1cb08b7d66f")) as ILocationSizeData;
            if (sizeData2 != null)
                expectedSize += sizeData2.ComputeSize(bundleLoc2);

            var assetLoc = new ResourceLocationBase("cachedSizeTestAsset", "myASset.asset",
                typeof(BundledAssetProvider).FullName, typeof(object), bundleLoc1, bundleLoc2);

            locMap.Add("cachedSizeTestBundle1", bundleLoc1);
            locMap.Add("cachedSizeTestBundle2", bundleLoc2);
            locMap.Add("cachedSizeTestAsset", assetLoc);
            m_Addressables.AddResourceLocator(locMap);

            var dOp = m_Addressables.GetDownloadSizeAsync(new List<object>()
                {
                    "cachedSizeTestAsset",
                    bundleLoc1,
                    bundleLoc2
                }
            );
            yield return dOp;
            Assert.IsTrue((bundleSize1 + bundleSize2) > dOp.Result);
            Assert.AreEqual(expectedSize, dOp.Result);

            dOp.Release();
            m_Addressables.RemoveResourceLocator(locMap);
            Directory.Delete(fakeCachePath, true);
#else
            Assert.Ignore();
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator GetDownloadSize_WithList_CalculatesCorrectSize_WhenAssetsReferenceSameBundle()
        {
#if ENABLE_CACHING
            yield return Init();
            long bundleSize1 = 1000;
            long expectedSize = 0;

            var bundleLoc1 = new ResourceLocationBase("sizeTestBundle1", "http://nowhere.com/GetDownloadSize_WithList_CalculatesCachedBundlesBundle1.bundle",
                typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData1 =
                (bundleLoc1.Data = CreateLocationSizeData("cachedSizeTestBundle1", bundleSize1, 123,
                    "0e38e35d2177c282d5d6a2e54a803aab")) as ILocationSizeData;
            if (sizeData1 != null)
                expectedSize += sizeData1.ComputeSize(bundleLoc1);

            var assetLoc1 = new ResourceLocationBase("cachedSizeTestAsset1", "myAsset1.asset",
                typeof(BundledAssetProvider).FullName, typeof(object), bundleLoc1);

            var assetLoc2 = new ResourceLocationBase("cachedSizeTestAsset2", "myAsset2.asset",
                typeof(BundledAssetProvider).FullName, typeof(object), bundleLoc1);

            var dOp = m_Addressables.GetDownloadSizeAsync(new List<object>()
                {
                    assetLoc1,
                    assetLoc2
                }
            );
            yield return dOp;
            Assert.IsTrue(bundleSize1 >= dOp.Result);
            Assert.AreEqual(expectedSize, dOp.Result);
#else
            Assert.Ignore();
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator GetDownloadSize_WithList_CalculatesCorrectSize_WhenAssetsReferenceDifferentBundle()
        {
#if ENABLE_CACHING
            yield return Init();
            long bundleSize1 = 1000;
            long bundleSize2 = 250;
            long expectedSize = 0;

            var bundleLoc1 = new ResourceLocationBase("sizeTestBundle1", "http://nowhere.com/GetDownloadSize_WithList_CalculatesCachedBundlesBundle1.bundle",
                typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData1 =
                (bundleLoc1.Data = CreateLocationSizeData("cachedSizeTestBundle1", bundleSize1, 123,
                    "0e38e35d2177c282d5d6a2e54a803aab")) as ILocationSizeData;
            if (sizeData1 != null)
                expectedSize += sizeData1.ComputeSize(bundleLoc1);

            var bundleLoc2 = new ResourceLocationBase("cachedSizeTestBundle2", "http://nowhere.com/GetDownloadSize_WithList_CalculatesCachedBundlesBundle2.bundle",
                typeof(AssetBundleProvider).FullName, typeof(object));
            var sizeData2 =
                (bundleLoc2.Data = CreateLocationSizeData("cachedSizeTestBundle2", bundleSize2, 123,
                    "09fe965a6b253fb9dbd3e1cb08b7d66f")) as ILocationSizeData;
            if (sizeData2 != null)
                expectedSize += sizeData2.ComputeSize(bundleLoc2);

            var assetLoc1 = new ResourceLocationBase("cachedSizeTestAsset1", "myAsset1.asset",
                typeof(BundledAssetProvider).FullName, typeof(object), bundleLoc1);

            var assetLoc2 = new ResourceLocationBase("cachedSizeTestAsset2", "myAsset2.asset",
                typeof(BundledAssetProvider).FullName, typeof(object), bundleLoc2);

            var dOp = m_Addressables.GetDownloadSizeAsync(new List<object>()
                {
                    assetLoc1,
                    assetLoc2
                }
            );
            yield return dOp;
            Assert.IsTrue((bundleSize1 + bundleSize2) >= dOp.Result);
            Assert.AreEqual(expectedSize, dOp.Result);
#else
            Assert.Ignore();
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator GetResourceLocationsWithCorrectKeyAndWrongTypeReturnsEmptyResult()
        {
            yield return Init();
            AsyncOperationHandle<IList<IResourceLocation>> op = m_Addressables.LoadResourceLocationsAsync("prefabs_evenBASE", typeof(Texture2D));
            yield return op;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op.Status);
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(op.Result.Count, 0);
            op.Release();
        }

        [UnityTest]
        public IEnumerator CanGetResourceLocationsWithSingleKey()
        {
            yield return Init();
            int loadCount = 0;
            int loadedCount = 0;
            var ops = new List<AsyncOperationHandle<IList<IResourceLocation>>>();
            foreach (var k in m_KeysHashSet)
            {
                loadCount++;
                AsyncOperationHandle<IList<IResourceLocation>> op = m_Addressables.LoadResourceLocationsAsync(k.Key, typeof(object));
                ops.Add(op);
                op.Completed += op2 =>
                {
                    loadedCount++;
                    Assert.IsNotNull(op2.Result);
                    Assert.AreEqual(k.Value, op2.Result.Count);
                };
                
            }
            foreach(var op in ops)
            {
                yield return op;
                op.Release();
            }
        }
        [UnityTest]
        public IEnumerator GetResourceLocationsMergeModesFailsWithNoKeys([Values(Addressables.MergeMode.UseFirst, Addressables.MergeMode.Intersection, Addressables.MergeMode.Union)]Addressables.MergeMode mode)
        {
            yield return Init();

            IList<IResourceLocation> results;
            var ret = m_Addressables.GetResourceLocations(new object[] { }, typeof(GameObject), mode, out results);
            Assert.IsFalse(ret);
            Assert.IsNull(results);
        }

        [UnityTest]
        public IEnumerator GetResourceLocationsMergeModesSucceedsWithSingleKey([Values(Addressables.MergeMode.UseFirst, Addressables.MergeMode.Intersection, Addressables.MergeMode.Union)]Addressables.MergeMode mode)
        {
            yield return Init();

            IList<IResourceLocation> results;
            var ret = m_Addressables.GetResourceLocations(new object[] { "prefabs_evenBASE" }, typeof(GameObject), mode, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);
        }

        [UnityTest]
        public IEnumerator GetResourceLocationsMergeModeUnionSucceedsWithValidKeys()
        {
            yield return Init();

            IList<IResourceLocation> results;
            var ret = m_Addressables.GetResourceLocations(new object[] { "prefabs_evenBASE" }, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);
            var evenCount = results.Count;

            ret = m_Addressables.GetResourceLocations(new object[] { "prefabs_oddBASE" }, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);
            var oddCount = results.Count;

            ret = m_Addressables.GetResourceLocations(new object[] { "prefabs_evenBASE", "prefabs_oddBASE" }, typeof(GameObject), Addressables.MergeMode.Union, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);
            Assert.AreEqual(oddCount + evenCount, results.Count);
        }

        [UnityTest]
        public IEnumerator GetResourceLocationsMergeModeUnionSucceedsWithInvalidKeys()
        {
            yield return Init();

            IList<IResourceLocation> results;
            var ret = m_Addressables.GetResourceLocations(new object[] { "prefabs_evenBASE" }, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);
            var evenCount = results.Count;

            ret = m_Addressables.GetResourceLocations(new object[] { "prefabs_oddBASE" }, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);
            var oddCount = results.Count;

            ret = m_Addressables.GetResourceLocations(new object[] { "prefabs_evenBASE", "prefabs_oddBASE", "INVALIDKEY" }, typeof(GameObject), Addressables.MergeMode.Union, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);
            Assert.AreEqual(oddCount + evenCount, results.Count);
        }



        [UnityTest]
        public IEnumerator GetResourceLocationsMergeModeIntersectionFailsIfNoResultsDueToIntersection()
        {
            yield return Init();
           
            IList<IResourceLocation> results;
            var ret = m_Addressables.GetResourceLocations(new object[] { "prefabs_evenBASE" }, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);

            ret = m_Addressables.GetResourceLocations(new object[] { "prefabs_oddBASE" }, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);

            ret = m_Addressables.GetResourceLocations(new object[] { "prefabs_evenBASE", "prefabs_oddBASE" }, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsFalse(ret);
            Assert.IsNull(results);
        }


        [UnityTest]
        public IEnumerator GetResourceLocationsMergeModeIntersectionFailsIfNoResultsDueToInvalidKey()
        {
            yield return Init();

            IList<IResourceLocation> results;
            var ret = m_Addressables.GetResourceLocations(new object[] { "prefabs_evenBASE" }, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);

            ret = m_Addressables.GetResourceLocations(new object[] { "prefabs_oddBASE" }, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsTrue(ret);
            Assert.NotNull(results);
            Assert.GreaterOrEqual(results.Count, 1);

            ret = m_Addressables.GetResourceLocations(new object[] { "prefabs_evenBASE", "prefabs_oddBASE", "INVALIDKEY" }, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsFalse(ret);
            Assert.IsNull(results);

            ret = m_Addressables.GetResourceLocations(new object[] { "prefabs_evenBASE", "INVALIDKEY" }, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsFalse(ret);
            Assert.IsNull(results);

            ret = m_Addressables.GetResourceLocations(new object[] { "INVALIDKEY" }, typeof(GameObject), Addressables.MergeMode.Intersection, out results);
            Assert.IsFalse(ret);
            Assert.IsNull(results);
        }

        [UnityTest]
        public IEnumerator WhenLoadWithInvalidKey_ReturnedOpIsFailed()
        {
            yield return Init();
            List<object> keys = new List<object>() { "INVALID1", "INVALID2" };
            AsyncOperationHandle<IList<GameObject>> gop = m_Addressables.LoadAssetsAsync<GameObject>(keys, null, Addressables.MergeMode.Intersection);
            while (!gop.IsDone)
                yield return null;
            Assert.IsTrue(gop.IsDone);
            Assert.AreEqual(AsyncOperationStatus.Failed, gop.Status);
            m_Addressables.Release(gop);
        }


        [UnityTest]
        public IEnumerator CanLoadAssetsWithMultipleKeysMerged()
        {
            yield return Init();
            List<object> keys = new List<object>() { AddressablesTestUtility.GetPrefabLabel("BASE"), AddressablesTestUtility.GetPrefabUniqueLabel("BASE", 0) };
            AsyncOperationHandle<IList<GameObject>> gop = m_Addressables.LoadAssetsAsync<GameObject>(keys, null, Addressables.MergeMode.Intersection);
            while (!gop.IsDone)
                yield return null;
            Assert.IsTrue(gop.IsDone);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, gop.Status);
            Assert.NotNull(gop.Result);
            Assert.AreEqual(1, gop.Result.Count);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, gop.Status);
            m_Addressables.Release(gop);
        }

        [UnityTest]
        public IEnumerator Release_WhenObjectIsUnknown_LogsErrorAndDoesNotDestroy()
        {
            yield return Init();
            GameObject go = Object.Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube));
            go.name = "TestCube";

            m_Addressables.Release(go);
            LogAssert.Expect(LogType.Error, new Regex("Addressables.Release was called on.*"));
            yield return null;

            GameObject foundObj = GameObject.Find("TestCube");
            Assert.IsNotNull(foundObj);
            Object.Destroy(foundObj);
        }

        [UnityTest]
        public IEnumerator ReleaseInstance_WhenObjectIsUnknown_LogsErrorAndDestroys()
        {
            yield return Init();
            GameObject go = Object.Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube));
            go.name = "TestCube";

            Assert.IsFalse(m_Addressables.ReleaseInstance(go));
        }

        [UnityTest]
        public IEnumerator LoadAsset_WhenEntryExists_ReturnsAsset()
        {
            yield return Init();
            string label = AddressablesTestUtility.GetPrefabUniqueLabel("BASE", 0);
            AsyncOperationHandle<GameObject> op = m_Addressables.LoadAssetAsync<GameObject>(label);
            yield return op;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op.Status);
            Assert.IsTrue(op.Result != null);
            op.Release();
        }

        [UnityTest]
        public IEnumerator LoadAssetWithWrongType_WhenEntryExists_Fails()
        {
            yield return Init();
            string label = AddressablesTestUtility.GetPrefabUniqueLabel("BASE", 0);
            AsyncOperationHandle<Texture> op = m_Addressables.LoadAssetAsync<Texture>(label);
            yield return op;
            Assert.AreEqual(AsyncOperationStatus.Failed, op.Status);
            Assert.IsNull(op.Result);
            op.Release();
        }

        [UnityTest]
        public IEnumerator LoadAsset_WhenEntryDoesNotExist_OperationFails()
        {
            yield return Init();
            AsyncOperationHandle<GameObject> op = m_Addressables.LoadAssetAsync<GameObject>("unknownlabel");
            yield return op;
            Assert.AreEqual(AsyncOperationStatus.Failed, op.Status);
            Assert.IsTrue(op.Result == null);
            op.Release();
        }

        [UnityTest]
        public IEnumerator LoadAsset_CanReleaseThroughAddressablesInCallback([Values(true, false)]bool addressableRelease)
        {
            yield return Init();
            var op = m_Addressables.LoadAssetAsync<object>(m_PrefabKeysList[0]);
            op.Completed += x =>
            {
                Assert.IsNotNull(x.Result);
                if (addressableRelease)
                    m_Addressables.Release(x.Result);
                else
                    op.Release();
            };
            yield return op;
        }

        [UnityTest]
        public IEnumerator LoadAsset_WhenPrefabLoadedAsMultipleTypes_ResultIsEqual()
        {
            yield return Init();

            string label = AddressablesTestUtility.GetPrefabUniqueLabel("BASE", 0);
            AsyncOperationHandle<object> op1 = m_Addressables.LoadAssetAsync<object>(label);
            AsyncOperationHandle<GameObject> op2 = m_Addressables.LoadAssetAsync<GameObject>(label);
            yield return op1;
            yield return op2;
            Assert.AreEqual(op1.Result, op2.Result);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op1.Status);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op2.Status);
            op1.Release();
            op2.Release();
        }

        [UnityTest]
        public IEnumerator LoadAssets_InvokesCallbackPerAsset()
        {
            yield return Init();
            string label = AddressablesTestUtility.GetPrefabLabel("BASE");
            HashSet<GameObject> ops = new HashSet<GameObject>();
            var gop = m_Addressables.LoadAssetsAsync<GameObject>(label, x => { ops.Add(x); });
            yield return gop;
            Assert.AreEqual(AddressablesTestUtility.kPrefabCount, ops.Count);
            for (int i = 0; i < ops.Count; i++)
                Assert.IsTrue(ops.Contains(gop.Result[i]));
            gop.Release();
        }


        // TODO: this doesn't actually check that something was downloaded. It is more: can load dependencies. 
        // We really need to address the downloading feature
        [UnityTest]
        public IEnumerator DownloadDependnecies_CanDownloadDependencies()
        {
            yield return Init();
            string label = AddressablesTestUtility.GetPrefabLabel("BASE");
            AsyncOperationHandle op = m_Addressables.DownloadDependenciesAsync(label);
            yield return op;
            op.Release();
        }

        [UnityTest]
        public IEnumerator DownloadDependnecies_AutoReleaseHandle_ReleasesOnCompletion()
        {
            yield return Init();
            string label = AddressablesTestUtility.GetPrefabLabel("BASE");
            AsyncOperationHandle op = m_Addressables.DownloadDependenciesAsync(label, true);
            yield return op;
            Assert.IsFalse(op.IsValid());
        }

        [UnityTest]
        public IEnumerator DownloadDependneciesWithAddress_AutoReleaseHandle_ReleasesOnCompletion()
        {
            yield return Init();
            AsyncOperationHandle op = m_Addressables.DownloadDependenciesAsync(m_PrefabKeysList[0], true);
            yield return op;
            Assert.IsFalse(op.IsValid());
        }

        [UnityTest]
        public IEnumerator DownloadDependnecies_DoesNotRetainLoadedBundles_WithAutoRelease()
        {
            yield return Init();
            int bundleCountBefore = AssetBundle.GetAllLoadedAssetBundles().Count();
            string label = AddressablesTestUtility.GetPrefabLabel("BASE");
            AsyncOperationHandle op = m_Addressables.DownloadDependenciesAsync(label, true);
            yield return op;
            Assert.AreEqual(bundleCountBefore, AssetBundle.GetAllLoadedAssetBundles().Count());
        }

        [UnityTest]
        public IEnumerator StressInstantiation()
        {
            yield return Init();

            // TODO: move this safety check to test fixture base
            var objs = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var r in objs)
                Assert.False(r.name.EndsWith("(Clone)"), "All instances from previous test were not cleaned up");

            var ops = new List<AsyncOperationHandle<GameObject>>();
            for (int i = 0; i < 50; i++)
            {
                var key = m_PrefabKeysList[i % m_PrefabKeysList.Count];
                ops.Add(m_Addressables.InstantiateAsync(key));
            }

            foreach(AsyncOperationHandle<GameObject> op in ops)
                yield return op;

            foreach (AsyncOperationHandle<GameObject> op in ops)
                m_Addressables.ReleaseInstance(op.Result);

            yield return null;

            objs = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var r in objs)
                Assert.False(r.name.EndsWith("(Clone)"), "All instances from this test were not cleaned up");
        }

        [UnityTest]
        public IEnumerator CanUnloadAssetReference_WithAddressables()
        {
            yield return Init();

            AsyncOperationHandle handle = m_Addressables.InstantiateAsync(AssetReferenceObjectKey);
            yield return handle;
            Assert.IsNotNull(handle.Result as GameObject);
            AssetReferenceTestBehavior behavior =
                (handle.Result as GameObject).GetComponent<AssetReferenceTestBehavior>();
            AsyncOperationHandle<GameObject> assetRefHandle = m_Addressables.InstantiateAsync(behavior.Reference);
            yield return assetRefHandle;
            Assert.IsNotNull(assetRefHandle.Result);

            string name = assetRefHandle.Result.name;
            Assert.IsNotNull(GameObject.Find(name));

            m_Addressables.ReleaseInstance(assetRefHandle.Result);
            yield return null;
            Assert.IsNull(GameObject.Find(name));

            handle.Release();
        }

        [UnityTest]
        public IEnumerator CanloadAssetReferenceSubObject()
        {
            yield return Init();

            var handle = m_Addressables.InstantiateAsync(AssetReferenceObjectKey);
            yield return handle;
            Assert.IsNotNull(handle.Result);
            AssetReferenceTestBehavior behavior = handle.Result.GetComponent<AssetReferenceTestBehavior>();

            AsyncOperationHandle<Object> assetRefHandle = m_Addressables.LoadAssetAsync<Object>(behavior.ReferenceWithSubObject);
            yield return assetRefHandle;
            Assert.IsNotNull(assetRefHandle.Result);
            m_Addressables.Release(assetRefHandle);
            handle.Release();
        }

        [UnityTest]
        public IEnumerator RuntimeKeyIsValid_ReturnsTrueForValidKeys()
        {
            yield return Init();

            AsyncOperationHandle handle = m_Addressables.InstantiateAsync(AssetReferenceObjectKey);
            yield return handle;
            Assert.IsNotNull(handle.Result as GameObject);
            AssetReferenceTestBehavior behavior =
                (handle.Result as GameObject).GetComponent<AssetReferenceTestBehavior>();

            Assert.IsTrue((behavior.Reference as IKeyEvaluator).RuntimeKeyIsValid());
            Assert.IsTrue((behavior.LabelReference as IKeyEvaluator).RuntimeKeyIsValid());

            handle.Release();
        }

        [UnityTest]
        public IEnumerator PercentComplete_CalculationIsCorrect_WhenInAGroupOperation()
        {
            yield return Init();
            GroupOperation groupOp = new GroupOperation();

            float handle1PercentComplete = 0.22f;
            float handle2PercentComplete = 0.78f;
            float handle3PercentComplete = 1.0f;
            float handle4PercentComplete = 0.35f;

            List<AsyncOperationHandle> handles = new List<AsyncOperationHandle>()
            {
                new ManualPercentCompleteOperation(handle1PercentComplete).Handle,
                new ManualPercentCompleteOperation(handle2PercentComplete).Handle,
                new ManualPercentCompleteOperation(handle3PercentComplete).Handle,
                new ManualPercentCompleteOperation(handle4PercentComplete).Handle
            };

            groupOp.Init(handles);

            Assert.AreEqual((handle1PercentComplete + handle2PercentComplete + handle3PercentComplete + handle4PercentComplete)/4, groupOp.PercentComplete);
        }

        [UnityTest]
        public IEnumerator PercentComplete_CalculationIsCorrect_WhenInAChainOperation()
        {
            yield return Init();

            float handle1PercentComplete = 0.6f;
            float handle2PercentComplete = 0.98f;

            AsyncOperationHandle<GameObject> slowHandle1 = new ManualPercentCompleteOperation(handle1PercentComplete).Handle;
            AsyncOperationHandle<GameObject> slowHandle2 = new ManualPercentCompleteOperation(handle2PercentComplete).Handle;

 //           while (!m_Addressables.InitializationOperation.IsDone)
 //               yield return null;

            slowHandle1.m_InternalOp.m_RM = m_Addressables.ResourceManager;
            slowHandle2.m_InternalOp.m_RM = m_Addressables.ResourceManager;

            var chainOperation = m_Addressables.ResourceManager.CreateChainOperation(slowHandle1, (op) =>
            {
                return slowHandle2;
            });
            
            chainOperation.m_InternalOp.Start(m_Addressables.ResourceManager, default, null);

            Assert.AreEqual((handle1PercentComplete + handle2PercentComplete) / 2, chainOperation.PercentComplete);
        }

        [UnityTest]
        public IEnumerator RuntimeKeyIsValid_ReturnsFalseForInValidKeys()
        {
            yield return Init();

            AsyncOperationHandle handle = m_Addressables.InstantiateAsync(AssetReferenceObjectKey);
            yield return handle;
            Assert.IsNotNull(handle.Result as GameObject);
            AssetReferenceTestBehavior behavior =
                (handle.Result as GameObject).GetComponent<AssetReferenceTestBehavior>();

            Assert.IsFalse((behavior.InValidAssetReference as IKeyEvaluator).RuntimeKeyIsValid());
            Assert.IsFalse((behavior.InvalidLabelReference as IKeyEvaluator).RuntimeKeyIsValid());

            handle.Release();
        }

        string CreateFakeCachedBundle(string bundleName, string hash)
        {
            string fakeCachePath = string.Format("{0}/{1}/{2}", Caching.defaultCache.path, bundleName, hash);
            Directory.CreateDirectory(fakeCachePath);
            var dataFile = File.Create(Path.Combine(fakeCachePath, "__data"));
            var infoFile = File.Create(Path.Combine(fakeCachePath, "__info"));

            byte[] info = new UTF8Encoding(true).GetBytes(
@"-1
1554740658
1
__data");
            infoFile.Write(info, 0, info.Length);

            dataFile.Dispose();
            infoFile.Dispose();

            return fakeCachePath;
        }
    }
}
