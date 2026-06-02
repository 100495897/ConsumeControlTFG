# ConsumeControlTFG

Herramienta de monitorización avanzada para entornos Windows desarrollada en C# (.NET). Este proyecto permite medir el consumo energético (W) de la CPU y GPU, estimar el consumo desglosado por procesos en ejecución y calcular la huella de carbono geolocalizada en tiempo real utilizando la API de Electricity Maps.

Proyecto desarrollado como Trabajo Fin de Grado (TFG) en Ingeniería Informática.

---

## Requisitos Previos (Muy Importante)

Debido a que esta aplicación interactúa con sensores físicos de hardware a muy bajo nivel (Ring 0), requiere ciertas configuraciones específicas para funcionar correctamente:

1. **Privilegios de Administrador:** La aplicación o el entorno de desarrollo (Visual Studio) **deben ejecutarse como Administrador**. De lo contrario, el acceso a los registros del procesador será bloqueado por el sistema operativo.
2. **Controlador PawnIO:** El motor de lectura de hardware depende del controlador de núcleo independiente `PawnIO`. Es necesario asegurar que este driver está instalado y permitido en el sistema (evitando bloqueos de Windows Defender o sistemas Anti-Cheat) para que la lectura de parámetros como el *Package Power* sea posible.
3. **Entorno Nativo:** No se puede ejecutar en máquinas virtuales (VirtualBox, VMware, WSL), ya que el hipervisor aísla el acceso directo a los sensores de la placa base.

## Configuración del Proyecto (Instalación)

Para proteger las credenciales de los servicios externos, la clave de la API de *Electricity Maps* no se incluye en este repositorio. Antes de compilar el proyecto por primera vez, debes configurar el archivo local siguiendo estos pasos:

1. Clona este repositorio en tu equipo local.
2. En la raíz del proyecto, busca el archivo llamado `App.config.example`.
3. Renombra ese archivo para que se llame exactamente **`App.config`**.
4. Abre el archivo y sustituye el texto `"API_KEY"` por una clave válida de la API de [Electricity Maps](https://www.electricitymaps.com/).

## Ejecución

Una vez configurado el App.config, abre la solución .sln en Visual Studio (iniciado como Administrador) y compila el proyecto.

