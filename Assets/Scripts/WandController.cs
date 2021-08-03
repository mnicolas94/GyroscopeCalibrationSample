using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class WandController : MonoBehaviour
    {
        [SerializeField] private Transform wand;
        
        private CalibrationData _calibrationData;

        private void Start()
        {
            _calibrationData = CalibrationData.Default();
        }

        public void SetCalibrationData(CalibrationData data)
        {
            _calibrationData = data;
        }

        private void Update()
        {
            var attitude = Input.gyro.attitude;
            var leftAttitude = attitude.SwapLeftRightCoordinateSystem();
            var calibrated = _calibrationData.Calibrate(leftAttitude);
            wand.rotation = calibrated;
        }
    }
}