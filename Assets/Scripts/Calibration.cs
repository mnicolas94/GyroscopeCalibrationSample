using UnityEngine;

public class Calibration : MonoBehaviour
{
    /// <summary>
    /// Procesar datos obtenidos durante la calibración.
    /// </summary>
    /// <param name="referenceFrames"></param> Quaternions de referencia para calibrar.
    /// Estos quaternions deben representar: orientación identidad, rotación hacia adelante de aproximadamente 45 grados
    /// y rotación hacia la derecha de aproximadamente 45 grados, en ese orden .
    /// <param name="calibTolerance"></param> Tolerancia para proyectar los ejes de rotación sobre los ejes cardinales.
    /// <param name="success"></param> parámetro booleano de salida para notificar si la calibración tuvo éxito.
    /// <returns></returns>
    public static CalibrationData ProcessData(Quaternion[] referenceFrames, float calibTolerance, out bool success)
    {
        var deviceUp = referenceFrames[0];
        var deviceFront = referenceFrames[1];
        var deviceRight = referenceFrames[2];
        
        var leftDeviceUp = deviceUp.SwapLeftRightCoordinateSystem();
        var leftDeviceFront = deviceFront.SwapLeftRightCoordinateSystem();
        var leftDeviceRight = deviceRight.SwapLeftRightCoordinateSystem();
        
        var tofront = FromTo(leftDeviceUp, leftDeviceFront).normalized;
        var toright = FromTo(leftDeviceUp, leftDeviceRight).normalized;

        bool xprojected;
        var xaxis = new Vector3(tofront.x, tofront.y, tofront.z).normalized;
        xaxis *= Mathf.Sign(tofront.w);
        var projectedXaxis = ProjectOnBasisAxis(xaxis, calibTolerance, out xprojected).normalized;
        
        bool zprojected;
        var zaxis = -(new Vector3(toright.x, toright.y, toright.z)).normalized;
        zaxis *= Mathf.Sign(toright.w);
        var projectedZaxis = ProjectOnBasisAxis(zaxis, calibTolerance, out zprojected).normalized;

        float dotproduct = Vector3.Dot(projectedXaxis, projectedZaxis);
        bool xortogonalz = Mathf.Abs(dotproduct) < 0.00001f;

        var yaxis = Vector3.Cross(zaxis, xaxis).normalized;
        var projectedYaxis = Vector3.Cross(projectedZaxis, projectedXaxis).normalized;
        
        Quaternion correction = Mat2Quat(projectedXaxis, projectedYaxis, projectedZaxis).normalized;
        
        CalibrationData data = new CalibrationData();
        data.up = deviceUp;
        data.front = deviceFront;
        data.right = deviceRight;
        data.leftUp = leftDeviceUp;
        data.leftFront = leftDeviceFront;
        data.leftRight = leftDeviceRight;
        data.xaxis = xaxis;
        data.yaxis = yaxis;
        data.zaxis = zaxis;
        data.projectedXaxis = projectedXaxis;
        data.projectedYaxis = projectedYaxis;
        data.projectedZaxis = projectedZaxis;
        data.axisCorrection = correction;

        success = xprojected && zprojected && xortogonalz;
        return data;
    }

    /// <summary>
    /// Rotación necesaria a aplicar para llegar del quaternion from al quaternion to.
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    private static Quaternion FromTo(Quaternion from, Quaternion to)
    {
        return Quaternion.Inverse(from) * to;
    }

    /// <summary>
    /// Convertir matriz de rotación en quaternion.
    /// </summary>
    /// <param name="xaxis"></param>
    /// <param name="yaxis"></param>
    /// <param name="zaxis"></param>
    /// <returns></returns>
    public static Quaternion Mat2Quat(Vector3 xaxis, Vector3 yaxis, Vector3 zaxis)
    {
        var m = Matrix4x4.zero;
        m.m00 = xaxis.x;
        m.m01 = xaxis.y;
        m.m02 = xaxis.z;
        m.m10 = yaxis.x;
        m.m11 = yaxis.y;
        m.m12 = yaxis.z;
        m.m20 = zaxis.x;
        m.m21 = zaxis.y;
        m.m22 = zaxis.z;
        m.m33 = 1;

        return m.rotation;
    }

    /// <summary>
    /// Proyectar un vector en el eje cardinal más cercano.
    /// </summary>
    /// <param name="axis"></param> eje a proyectar
    /// <param name="tolerance"></param> tolerancia para decidir si el vector está lo suficientemente cerca de un eje cardinal.
    /// <returns></returns>
    private static Vector3 ProjectOnBasisAxis(Vector3 axis, float tolerance, out bool projected)
    {
        projected = true;
        float x = axis.x;
        float y = axis.y;
        float z = axis.z;
        
        float x2 = x * x;
        float y2 = y * y;
        float z2 = z * z;
        
        float tx = x2 * tolerance;
        float ty = y2 * tolerance;
        float tz = z2 * tolerance;

        if (tx > y2 + z2)
        {
            return new Vector3(x, 0, 0);
        }
        if (ty > x2 + z2)
        {
            return new Vector3(0, y, 0);
        }
        if (tz > x2 + y2)
        {
            return new Vector3(0, 0, z);
        }

        projected = false;
        return axis;
    }
}


public struct CalibrationData
{
    public Quaternion up;
    public Quaternion front;
    public Quaternion right;
    
    private Quaternion _leftUp;
    public Quaternion leftUp
    {
        get { return _leftUp; }
        set
        {
            _leftUp = value;
            _leftUpCorrected = axisCorrection * Quaternion.Inverse(value);
        }
    }
    public Quaternion leftFront;
    public Quaternion leftRight;

    public Vector3 xaxis;
    public Vector3 yaxis;
    public Vector3 zaxis;
    
    public Vector3 projectedXaxis;
    public Vector3 projectedYaxis;
    public Vector3 projectedZaxis;

    private Quaternion _axisCorrection;

    public Quaternion axisCorrection
    {
        get { return _axisCorrection; }
        set
        {
            _axisCorrection = value;
            _axisCorrectionInverse = Quaternion.Inverse(value);
            _leftUpCorrected = value * Quaternion.Inverse(leftUp);
        }
    }
    private Quaternion _axisCorrectionInverse;
    public Quaternion axisCorrectionInverse => _axisCorrectionInverse;
    private Quaternion _leftUpCorrected;
    public Quaternion leftUpCorrected => _leftUpCorrected;

    public static CalibrationData Default()
    {
        var def = new CalibrationData();
        
        def.up = new Quaternion(0, 0, 0, 1);
        def.front = new Quaternion(1, 0, 0, 1);
        def.right = new Quaternion(0, 0, 1, 1);
        def.leftUp = new Quaternion(0, 0, 0, -1);
        def.leftFront = new Quaternion(1, 0, 0, -1);
        def.leftRight = new Quaternion(0, 0, -1, -1);
        def.xaxis = new Vector3(1, 0, 0);
        def.yaxis = new Vector3(0, 1, 0);
        def.zaxis = new Vector3(0, 0, 1);
        def.projectedXaxis = new Vector3(1, 0, 0);
        def.projectedYaxis = new Vector3(0, 1, 0);
        def.projectedZaxis = new Vector3(0, 0, 1);
        def.axisCorrection = new Quaternion(0, 0, 0, 1);
        
        return def;
    }

    public Quaternion Calibrate(Quaternion q)
    {
        return leftUpCorrected * q * axisCorrectionInverse;  // c * U^-1 * q * c^-1
    }
}
