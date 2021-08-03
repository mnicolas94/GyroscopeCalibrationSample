using System;
using UnityEngine;

public class CalibrationProcess
{
    private const int StepsCount = 3;

    public Action<CalibrationData> eventCalibrationFinished;
    public Action eventBadCalibration;
        
    public float calibTolerance = 0.4f;

    private bool _calibrating;
    private IAttitudeProvider _attitudeProvider;
    private int _calibratingStep;
    private Quaternion[] _recordedFrames;

    public CalibrationProcess()
    {
        _calibrating = false;
        _attitudeProvider = null;
        _calibratingStep = 0;
        _recordedFrames = new Quaternion[StepsCount];
    }

    public bool IsCalibrating()
    {
        return _calibrating;
    }

    public void StartCalibration(IAttitudeProvider attitudeProvider)
    {
        if (!_calibrating)
        {
            _calibrating = true;
            _attitudeProvider = attitudeProvider;
            _calibratingStep = 0;
        }
    }

    public void StopCalibration()
    {
        _calibrating = false;
        _attitudeProvider = null;
        _calibratingStep = 0;
    }
    
    private void FinishCalibration()
    {
        _calibrating = false;
        bool succes;
        CalibrationData data = Calibration.ProcessData(_recordedFrames, calibTolerance, out succes);
        if (succes)
        {
            eventCalibrationFinished?.Invoke(data);
        }
        else
        {
            eventBadCalibration?.Invoke();
        }
    }

    /// <summary>
    /// Records the device orientat1ion and moves to the next calibration step. If there is no more calibration steps
    /// the process finishes and the calibration is computed.
    /// </summary>
    /// <returns></returns>
    public bool NextCalibrationStep()
    {
        if (_calibrating)
        {
            Quaternion attitude = _attitudeProvider.GetAttitude();
            _recordedFrames[_calibratingStep] = attitude;
            _calibratingStep++;
            if (_calibratingStep == StepsCount)
            {
                // calibration finished
                FinishCalibration();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Go to the previous calibration step. If there is no previous step then the calibration process finishes. 
    /// </summary>
    /// <returns></returns> returns whether the calibration process continues or not.
    public bool PreviousCalibrationStep()
    {
        _calibratingStep--;
        if (_calibratingStep < 0)
        {
            StopCalibration();
            return false;
        }
        return true;
    }
}
