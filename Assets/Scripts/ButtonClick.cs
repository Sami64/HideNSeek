using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;

public class ButtonClick : MonoBehaviour
{
    public void OnPointerDown(PointerEventData eventData) {
        PlayClip("ButtonUp");
    }

    public void OnPointerUp(PointerEventData eventData) {
        PlayClip("ButtonDown");
    }
    
    private static void PlayClip(string n) {
            var clip = Addressables.LoadAssetAsync<AudioClip>(n).WaitForCompletion();
            AudioSource.PlayClipAtPoint(clip, Vector3.zero);
        }
}
