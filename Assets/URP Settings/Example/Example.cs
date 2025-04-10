using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example : MonoBehaviour
{
	[SerializeField] private float rotateSpeed;
	[SerializeField] private float moveSpeed;
	[SerializeField] private float moveDist;
	[SerializeField] private Transform rotateCube;
	[SerializeField] private Transform moveCube;

	private Vector3 moveCubeOffset;
	private float t = 0;

	private void Start()
	{
		moveCubeOffset = moveCube.transform.position;
	}

	private void Update()
	{
		t += Time.deltaTime;
		rotateCube.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
		moveCube.transform.position = moveCubeOffset + Vector3.Lerp(Vector3.up * moveDist, Vector3.down * moveDist, moveSpeed * t % 1f);
	}
}
