using DefaultNamespace;
using UnityEngine;
using UnityEngine.UI;

public class UICalibrationMenu : MonoBehaviour, IAttitudeProvider
{
    public GameObject step1Text;
    public GameObject step2Text;
    public GameObject step3Text;

    [SerializeField] private Button nextButton;
    [SerializeField] private WandController wandController;

    private GameObject _currentActive;
    private CalibrationProcess _calibrationProcess;
    
    void Start()
    {
        _calibrationProcess = new CalibrationProcess();
        _calibrationProcess.eventCalibrationFinished += UpdateCalibrationData;
        Input.gyro.enabled = true;  // habilitar el giroscopio
        
        DisableAll();
        nextButton.onClick.AddListener(OnNextStepClicked);
        nextButton.gameObject.SetActive(false);
    }

    private void UpdateCalibrationData(CalibrationData data)
    {
        wandController.SetCalibrationData(data);
    }

    public void StartCalibration()
    {
        _calibrationProcess.StartCalibration(this);
        DisableAll();
        EnableStepWindow(step1Text);
        nextButton.gameObject.SetActive(true);
    }

    private void OnNextStepClicked()
    {
        NextCalibrationStep();
    }
    
    private void NextCalibrationStep()
    {
        _calibrationProcess.NextCalibrationStep();
        if (_currentActive == step1Text)
        {
            EnableStepWindow(step2Text);
        }
        else if (_currentActive == step2Text)
        {
            EnableStepWindow(step3Text);
        }
        else if (_currentActive == step3Text)
        {
            DisableAll();
            nextButton.gameObject.SetActive(false);
        }
    }

    private void EnableStepWindow(GameObject step)
    {
        DisableAll();
        step.SetActive(true);
        _currentActive = step;
    }
    
    private void DisableAll()
    {
        step1Text.SetActive(false);
        step2Text.SetActive(false);
        step3Text.SetActive(false);
    }

    public Quaternion GetAttitude()
    {
        return Input.gyro.attitude;
    }
}
