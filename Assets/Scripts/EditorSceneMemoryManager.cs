
#if UNITY_EDITOR
using UnityEditor;
//using UnityEditor.SceneManagement;
#endif
using UnityEngine.SceneManagement;
using System;

public static class EditorSceneMemoryManager
{

    //static EditorSceneMemoryManager()
    //{
    //    EditorSceneManager.sceneOpened += OnSceneOpened;
    //}

    //static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    //{
    //    GarbageCollect();
    //}

    //[MenuItem("Tools/Force Garbage Collection")]
    //static void GarbageCollect()
    //{
    //    EditorUtility.UnloadUnusedAssetsImmediate();
    //    GC.Collect();
    //}
}
