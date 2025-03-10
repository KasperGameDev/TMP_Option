using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example : MonoBehaviour
{
	[SerializeField] private float rotateSpeed;
	[SerializeField] private Transform rotatingCube;

	private void Update()
	{
		rotatingCube.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
	}
}
