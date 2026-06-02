# Datos y Validaciones

Esta carpeta contiene las exportaciones en crudo (archivos `.CSV`) generadas por la herramienta de monitorización durante la fase de validación del Trabajo Fin de Grado. 

El objetivo de estos datos es documentar el impacto energético del software en distintas arquitecturas bajo condiciones de carga estandarizadas.

## Entornos de Prueba (Hardware)

Para garantizar la representatividad del estudio, las mediciones se han llevado a cabo en cuatro equipos con capacidades técnicas muy distintas, abarcando desde equipos de sobremesa de alto rendimiento hasta portátiles ofimáticos sin aceleración gráfica dedicada:

| Equipo | Tipo | Procesador (CPU) | Tarjeta Gráfica (GPU) |
| :--- | :--- | :--- | :--- |
| **Portátil 1** | Portátil (Gaming) | 13th Gen Intel Core i7-13620H | NVIDIA GeForce RTX 4050 Laptop GPU |
| **Portátil 2** | Portátil (Ofimática) | Intel Core i5-4210U @ 1.70GHz | *Sin gráfica dedicada (Solo iGPU)* |
| **PC 1** | Sobremesa (Gama Media) | 11th Gen Intel Core i5-11600K @ 3.90GHz | NVIDIA GeForce RTX 3060 |
| **PC 2** | Sobremesa (Gama Alta) | Intel Core i7-9700K @ 3.60GHz | NVIDIA GeForce RTX 2070 SUPER |

## Protocolo de Medición (Escenarios)

Cada equipo ha sido sometido a cuatro pruebas consecutivas de **15 minutos** cada una, diseñadas para estresar distintos componentes del sistema. 

* **Reposo (Línea Base):** El equipo permanece inactivo sin interacción del usuario, ejecutando en segundo plano únicamente la herramienta de monitorización. Permite establecer el consumo mínimo operativo del sistema (W).
* **Ofimática (Multimedia y Lectura):** Simulación de trabajo de oficina. El equipo mantiene abierto el *Manual del Desarrollador de Intel* en Adobe Acrobat utilizando la función de Desplazamiento Automático (Auto-scroll). En paralelo, se reproduce un vídeo de paisajes en YouTube a resolución 1080p ([Norway 4K • Scenic Relaxation Film](https://www.youtube.com/watch?v=KLuTLF3x9sA)).
* **Gaming (Renderizado Gráfico):** Ejecución del videojuego gratuito *Asphalt Legends* para evaluar el consumo de la GPU. Los fotogramas se bloquean a 60 FPS en todos los dispositivos para estandarizar la carga lógica, adaptando la calidad gráfica al hardware:
  * **PC 1 y PC 2:** Calidad gráfica configurada en *Ultra*.
  * **Portátil 1:** Calidad gráfica configurada en *Alta*.
  * **Portátil 2:** Prueba restringida a la renderización de la pantalla de inicio debido a la incapacidad del hardware para cargar el motor del juego.
* **Estrés (Carga Sintética 3D):** Ejecución en bucle de la carga de trabajo *Extreme Test* alojada en el benchmark WebGL [Silver Urih](https://silver.urih.com/) para provocar un escenario de demanda térmica y eléctrica sostenida hasta agotar los 15 minutos de la prueba.

## Condiciones de la Red Eléctrica

Para dotar de coherencia al cálculo de la huella de carbono ($gCO_2/kWh$), todas las sesiones de monitorización documentadas en esta carpeta fueron ejecutadas en la misma ventana horaria, comprendida entre las **16:00 y las 18:00 horas**, estableciendo España como la región de referencia para obtener la intensidad de la red eléctrica (grid).