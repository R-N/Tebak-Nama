using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

public class MyScrollRect : ScrollRect, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler {
	bool inside = false;

	float prevDist = 0;

	 void Update(){
		if (inside)
			content.localScale *= 1 + Input.mouseScrollDelta.y / 100.0f;
	}

	void Zoom(){
		float dist = Vector2.Distance (Input.GetTouch (0).position, Input.GetTouch (1).position);
		content.localScale *= 1 + (dist - prevDist) / 100.0f;
		prevDist = dist;
	}

	public void Copy(MyScrollRect input){
		content.GetComponent<RawImage> ().texture = input.content.GetComponent<RawImage> ().texture;
		content.localScale = input.content.localScale;
		normalizedPosition = input.normalizedPosition;
	}

	public void OnPointerDown(PointerEventData data){
		if (Input.touchCount == 2) {
			prevDist = Vector2.Distance (Input.GetTouch (0).position, Input.GetTouch (1).position);
		}
	}

	public void OnPointerEnter(PointerEventData data){
		inside = true;
	}
	public void OnPointerExit(PointerEventData data){
		inside = false;
	}

	public override void OnBeginDrag(PointerEventData data){
		if (Input.touchCount == 2) {
			Zoom ();
		} else {
			base.OnBeginDrag (data);
		}
	}

	public override void OnDrag(PointerEventData data){
		if (Input.touchCount == 2) {
			Zoom ();
		} else {
			base.OnDrag (data);
		}
	}
	public override void OnEndDrag(PointerEventData data){
		if (Input.touchCount == 2) {
			Zoom ();
		} else {
			base.OnEndDrag (data);
		}
	}
}
