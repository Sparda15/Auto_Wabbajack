# Auto Wabbajack

**Fork NO OFICIAL de [Wabbajack](https://github.com/wabbajack-tools/wabbajack), basado en la versión 4.2.3.0, que añade funciones opcionales para facilitar y supervisar las descargas.**

Wabbajack fue creado por **[halgari](https://github.com/halgari)** y es desarrollado por el **[equipo Wabbajack y sus colaboradores](https://github.com/wabbajack-tools/wabbajack/graphs/contributors)**. El crédito por la aplicación original y su tecnología corresponde a sus autores. Este fork, mantenido por [Sparda15](https://github.com/Sparda15), se centra en las funciones adicionales descritas aquí y no es una distribución oficial ni implica el respaldo del equipo original.

## Proyecto original y créditos

- **Web oficial:** [wabbajack.org](https://www.wabbajack.org/)
- **Repositorio original:** [wabbajack-tools/wabbajack](https://github.com/wabbajack-tools/wabbajack)
- **Documentación oficial:** [wiki.wabbajack.org](https://wiki.wabbajack.org/)
- **Versión utilizada como base:** [Wabbajack 4.2.3.0](https://github.com/wabbajack-tools/wabbajack/releases/tag/4.2.3.0)

## Funciones añadidas por este fork

- **Auto Download opcional:** botón ON/OFF dentro del navegador integrado. Empieza en OFF al abrir la aplicación y mantiene la elección entre archivos durante esa sesión.
- **Selección de descarga:** identifica el botón Slow download o Fast download según la cuenta, y Standard download en el diálogo conocido de archivos grandes.
- **Estado visible:** Esperando página, Esperando descarga y Necesita intervención.
- **Avisos de bloqueo:** aviso visual y sonoro cuando se detecta un CAPTCHA, una sesión cerrada, un error de navegación o un diálogo pendiente. También avisa tras 45 segundos sin inicio de descarga.

Se conserva el flujo original de instalación, cola, descarga y verificación de archivos. El lanzador y su actualizador apuntan a las releases de este fork. No se distribuyen mods con la aplicación.

La automatización utiliza controles identificados de la página; no elimina límites de velocidad, esperas ni requisitos de cuenta. El inicio de sesión y los CAPTCHA requieren intervención manual. Si cambia la estructura de Nexus, puede ser necesario continuar manualmente.

## Descargar y utilizar

**[Descargar Auto Wabbajack 1.0 estable](https://github.com/Sparda15/Auto_Wabbajack/releases/tag/1.0.0.0)**

1. Descarga **Wabbajack.exe** de los archivos de la release.
2. Colócalo en una carpeta nueva, separada de la instalación oficial.
3. Ejecuta ese archivo: el lanzador descargará la aplicación y creará su carpeta de versión.

La release incluye el lanzador autocontenido para Windows x64, **1.0.0.0.zip** con la aplicación y CLI, y **SHA256SUMS.txt**. Para instalar manualmente, extrae el ZIP en una subcarpeta llamada `1.0.0.0` junto al lanzador. Los ejecutables de este fork no tienen la firma digital del proyecto oficial.

La versión 1.0 identifica las funciones de este fork; la aplicación mantiene su base 4.2.3.0. [Detalles técnicos, compilación y pruebas](https://github.com/Sparda15/Auto_Wabbajack/blob/auto-nexus-4.2.3.0/AUTO-WABBAJACK.md).

## Soporte y licencia

Para incidencias de las funciones añadidas, utiliza [las incidencias de este fork](https://github.com/Sparda15/Auto_Wabbajack/issues). El equipo original no es responsable de estas modificaciones.

Se conserva la [licencia GPL-3.0 del proyecto original](LICENSE.txt) y los avisos de autoría y licencias de terceros. Gracias a halgari, al equipo Wabbajack y a todos sus colaboradores por hacer posible este proyecto.

---

<details>
<summary>Documentación original de Wabbajack</summary>

# Wabbajack

[![Discord](https://img.shields.io/discord/605449136870916175)](https://www.wabbajack.org/discord)
[![CI Tests](https://github.com/wabbajack-tools/wabbajack/actions/workflows/tests.yaml/badge.svg)](https://github.com/wabbajack-tools/wabbajack/actions/workflows/tests.yaml)
[![GitHub all releases](https://img.shields.io/github/downloads/wabbajack-tools/wabbajack/total)](https://github.com/wabbajack-tools/wabbajack/releases)

## License & Copyright

All original code in Wabbajack is given freely via the [GPL3 license](LICENSE.txt). Parts of Wabbajack use libraries that carry their own Open Sources licenses, those parts retain their original copyrights. Selling of Modlist files is strictly forbidden. As is hosting the files behind any sort of paywall. You received this tool free of charge, respect this by giving freely as you were given.


</details>
