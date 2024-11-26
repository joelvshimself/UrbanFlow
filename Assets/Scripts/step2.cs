using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Step2 : MonoBehaviour
{
    public string csvFileName = "l26.csv"; // Nombre del archivo CSV
    private Dictionary<string, List<Vector3>> waypoints = new Dictionary<string, List<Vector3>>();
    private List<Vector3> agentWaypoints;
    private int currentWaypointIndex = 0;
    private bool isReady = false;

    public float interval = 2.0f; // Intervalo en segundos para alcanzar el siguiente punto
    private float timer;

    void Awake()
    {
        LoadCSV();
    }

    void Start()
    {
        string entityName = gameObject.name.Trim().ToLower();

        // Verificar si los datos del CSV están listos
        if (!isReady)
        {
            Debug.LogError("Los datos del CSV aún no están cargados. Verifica la inicialización del CSV.");
            return;
        }

        // Obtener los waypoints del agente
        if (waypoints.TryGetValue(entityName, out agentWaypoints))
        {
            if (agentWaypoints.Count > 0)
            {
                Debug.Log($"Waypoints cargados correctamente para '{entityName}': {agentWaypoints.Count} puntos encontrados.");
                transform.position = agentWaypoints[0]; // Establecer posición inicial del agente
            }
            else
            {
                Debug.LogWarning($"No se encontraron waypoints para la entidad '{entityName}'.");
            }
        }
        else
        {
            Debug.LogError($"El CSV no contiene datos para la entidad '{entityName}'. Verifica el archivo.");
        }
    }

    void Update()
    {
        if (agentWaypoints == null || agentWaypoints.Count == 0) return;

        timer += Time.deltaTime;

        if (timer >= interval)
        {
            timer = 0f;

            if (currentWaypointIndex + 1 < agentWaypoints.Count)
            {
                Vector3 currentPos = agentWaypoints[currentWaypointIndex];
                Vector3 nextPos = agentWaypoints[currentWaypointIndex + 1];

                // Calcular velocidad como distancia / intervalo
                float speed = (nextPos - currentPos).magnitude / interval;

                // Si la velocidad es negativa (retroceso), teletransportar al siguiente punto
                if (speed < 0)
                {
                    Debug.Log($"Teletransportando '{gameObject.name}' al waypoint {currentWaypointIndex + 1}");
                    currentWaypointIndex++;
                    transform.position = nextPos;
                }
                else
                {
                    StartCoroutine(MoveSmoothlyToNextWaypoint(nextPos));
                    currentWaypointIndex++;
                }
            }
        }
    }

    private System.Collections.IEnumerator MoveSmoothlyToNextWaypoint(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < interval)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / interval);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition; // Asegurarse de que esté exactamente en el objetivo
    }

    void LoadCSV()
    {
        TextAsset csvFile = Resources.Load<TextAsset>(Path.GetFileNameWithoutExtension(csvFileName));
        if (csvFile == null)
        {
            Debug.LogError($"Archivo CSV {csvFileName} no encontrado en la carpeta Resources.");
            return;
        }

        string[] lines = csvFile.text.Split('\n'); // Dividir por líneas
        if (lines.Length < 2) return;

        // Leer encabezados de las entidades
        string[] headers = lines[0].Split(',');
        for (int i = 1; i < headers.Length; i += 3) // Cada entidad tiene tres columnas (X, Y, Z)
        {
            string entityName = headers[i].Replace("_X", "").Trim().ToLower();
            if (!string.IsNullOrEmpty(entityName))
            {
                waypoints[entityName] = new List<Vector3>();
            }
        }

        // Leer coordenadas de las entidades
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = lines[i].Split(',');
            int entityIndex = 1;

            foreach (var entityName in waypoints.Keys)
            {
                if (entityIndex + 2 < values.Length)
                {
                    float x = float.TryParse(values[entityIndex], out x) ? x : 0;
                    float y = float.TryParse(values[entityIndex + 1], out y) ? y : 0;
                    float z = float.TryParse(values[entityIndex + 2], out z) ? z : 0;

                    waypoints[entityName].Add(new Vector3(x, y, z));
                }
                entityIndex += 3;
            }
        }

        isReady = true;
        Debug.Log("CSV cargado correctamente.");
    }
}
