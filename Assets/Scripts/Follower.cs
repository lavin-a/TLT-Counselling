/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using System.Collections;
using UnityEngine;

namespace OculusSampleFramework
{
	public class Follower : MonoBehaviour
	{
		private const float TOTAL_DURATION = 3.0f;
		private const float HMD_MOVEMENT_THRESHOLD = 0.3f;
        private const float HMD_ROTATION_THRESHOLD = 1.0f;

        [SerializeField] private float _maxDistance = 0.3f;
		[SerializeField] private float _minDistance = 0.05f;
		[SerializeField] private float _minZDistance = 0.05f;
        [SerializeField] private float _maxRotation = 30.0f;
        [SerializeField] private float _minRotation = 1.0f;
        [SerializeField] private float _minZRotation = 1.0f;
        [SerializeField] private Camera _cameraRig;
        [SerializeField] private GameObject _postarget;
        [SerializeField] private GameObject _rottarget;

        private Vector3 _panelInitialPosition = Vector3.zero;
        private Vector3 _panelInitialRotation = Vector3.zero;
        private Coroutine _coroutine = null;
		private Vector3 _prevPos = Vector3.zero;
        private Quaternion _prevRot = Quaternion.identity;
        private Vector3 _lastMovedToPos = Vector3.zero;
        private Vector3 _lastMovedToRot = Vector3.zero;

        private void Awake()
		{
			_panelInitialPosition = _postarget.transform.position;
		}

		private void Update()
		{
            var centerEyeAnchorPos = _cameraRig.transform.position;
            var centerEyeAnchorRot = _cameraRig.transform.rotation;
            var myPosition = _postarget.transform.position;
            var myRotation = _rottarget.transform.rotation;
            //Distance from centereye since last time we updated panel position.
            float distanceFromLastMovement = Vector3.Distance(centerEyeAnchorPos, _lastMovedToPos);
            float rotationFromLastMovement = Quaternion.Angle(centerEyeAnchorRot, Quaternion.Euler(_lastMovedToRot));
            float headMovementSpeed = (_cameraRig.transform.position - _prevPos).magnitude / Time.deltaTime;
            float headRotationSpeed = Quaternion.Angle(_cameraRig.transform.rotation, _prevRot) / Time.deltaTime;
            var currDiffFromCenterEye = _postarget.transform.position - centerEyeAnchorPos;
            var currDiffFromCenterEyeRot = _rottarget.transform.rotation * Quaternion.Inverse(centerEyeAnchorRot);
            var currDistanceFromCenterEye = currDiffFromCenterEye.magnitude;
            var currDistanceFromCenterEyeRot = currDiffFromCenterEyeRot.eulerAngles.magnitude;


            // 1) wait for center eye to stabilize after distance gets too large
            // 2) check if center eye is too close to panel
            // 3) check if depth isn't too close
            if (((distanceFromLastMovement > _maxDistance) || (_minZDistance > currDiffFromCenterEye.z) || (_minDistance > currDistanceFromCenterEye)) || rotationFromLastMovement > _maxRotation || _minZRotation > currDiffFromCenterEyeRot.z || _minRotation > currDistanceFromCenterEyeRot
                &&
				headMovementSpeed < HMD_MOVEMENT_THRESHOLD || headRotationSpeed < HMD_ROTATION_THRESHOLD && _coroutine == null)
			{
				if (_coroutine == null)
				{
					_coroutine = StartCoroutine(LerpToHMD());
				}
			}

            _prevPos = _cameraRig.transform.position;
            _prevRot = _cameraRig.transform.rotation;
        }

		private Vector3 CalculateIdealAnchorPosition()
		{
			return _cameraRig.transform.position + _panelInitialPosition;
		}

        private Vector3 CalculateIdealAnchorRotation()
        {
            return _cameraRig.transform.rotation.eulerAngles + _panelInitialRotation;
        }

        private IEnumerator LerpToHMD()
		{
			Vector3 newPanelPosition = CalculateIdealAnchorPosition();
            Vector3 newPanelRotation = CalculateIdealAnchorRotation();
            _lastMovedToPos = _cameraRig.transform.position;
            _lastMovedToRot = _cameraRig.transform.rotation.eulerAngles;
            float startTime = Time.time;
			float endTime = Time.time + TOTAL_DURATION;

			while (Time.time < endTime)
			{
                _postarget.transform.position =
				  Vector3.Lerp(_postarget.transform.position, newPanelPosition, (Time.time - startTime) / TOTAL_DURATION);
                _rottarget.transform.rotation = Quaternion.Lerp(_rottarget.transform.rotation, _cameraRig.transform.rotation, (Time.time - startTime) / TOTAL_DURATION);
                yield return null;
			}

            _postarget.transform.position = newPanelPosition;
            _rottarget.transform.rotation = Quaternion.Euler(newPanelRotation);
            _coroutine = null;
		}
	}
}
