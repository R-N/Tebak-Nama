using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class SearchEntry : MonoBehaviour, IPointerClickHandler {
	public MainScript main = null;
	public long nrp = 0;
	public Text text = null;

	public void OnPointerClick(PointerEventData data){
		OnClick ();
	}

	public void OnClick(){
		main.TampilkanInfo (nrp);
		main.backToCari.SetActive (true);
	}
}
