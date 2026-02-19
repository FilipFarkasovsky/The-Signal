using Riptide;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Riptide.Demos.PlayerHosted
{
    public class GameLogicShared : MonoBehaviour
    {
        protected static byte activeScene;

        public void LoadScene(byte sceneBuildIndex)
        {
            StartCoroutine(SetSceneCoroutine(sceneBuildIndex));
        }

        protected IEnumerator SetSceneCoroutine(byte sceneBuildIndex)
        {
            if (sceneBuildIndex == activeScene)
                yield break;

            if (activeScene > 0)
                SceneManager.UnloadSceneAsync(activeScene);

            activeScene = sceneBuildIndex;

            if(sceneBuildIndex > 0)
            {
                AsyncOperation loadingScene = SceneManager.LoadSceneAsync(sceneBuildIndex, LoadSceneMode.Additive);
                while (!loadingScene.isDone)
                    yield return new WaitForSeconds(0.25f);
            }

            SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(sceneBuildIndex));
        }

        public static void UnloadActiveScene()
        {
            if (activeScene > 0)
            {
                SceneManager.UnloadSceneAsync(activeScene);
                activeScene = 0;
            }
        }
    }
}