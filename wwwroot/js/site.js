// ==========================================================================
// PAE - PORTAL DE AUTOGESTIÓN EDUCATIVA
// NÚCLEO DE COMUNICACIÓN, ALERTAS AMIGABLES Y SISTEMA DE AYUDA (1.5 & 1.6)
// ==========================================================================

window.PAE = window.PAE || {};

(function () {
    // --- 1.6: COMUNICACIÓN AMIGABLE Y NOTIFICACIONES CLARAS ---

    // Notificación flotante (Toast)
    PAE.toast = function (mensaje, tipo) {
        tipo = tipo || 'success';
        const icono = (tipo === 'error') ? 'error' : (tipo === 'warning') ? 'warning' : 'success';
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: icono,
                title: mensaje,
                showConfirmButton: false,
                timer: 3500,
                timerProgressBar: true
            });
        } else {
            console.log(`[PAE ${tipo.toUpperCase()}]: ${mensaje}`);
        }
    };

    // Mensaje de éxito comprensible para el usuario final
    PAE.exito = function (mensaje, titulo) {
        mensaje = mensaje || 'Operación completada exitosamente.';
        titulo = titulo || '¡Operación Exitosa!';
        if (typeof Swal !== 'undefined') {
            return Swal.fire({
                icon: 'success',
                title: titulo,
                text: mensaje,
                confirmButtonColor: '#0d2a4a',
                confirmButtonText: '<i class="bi bi-check-lg me-1"></i> Aceptar'
            });
        } else {
            alert(mensaje);
        }
    };

    // Mensaje de error claro y sin tecnicismos
    PAE.error = function (mensaje, titulo) {
        mensaje = mensaje || 'No fue posible completar la acción. Verifique los datos e inténtelo nuevamente.';
        titulo = titulo || 'Atención';
        if (typeof Swal !== 'undefined') {
            return Swal.fire({
                icon: 'error',
                title: titulo,
                text: mensaje,
                confirmButtonColor: '#0d2a4a',
                confirmButtonText: '<i class="bi bi-arrow-repeat me-1"></i> Entendido'
            });
        } else {
            alert(mensaje);
        }
    };

    // Confirmación amigable para sustituir los confirm() nativos del navegador
    PAE.confirmar = function (pregunta, detalle, callback) {
        pregunta = pregunta || '¿Está seguro de que desea continuar?';
        detalle = detalle || 'Esta acción no se puede deshacer.';
        if (typeof Swal !== 'undefined') {
            return Swal.fire({
                icon: 'warning',
                title: pregunta,
                text: detalle,
                showCancelButton: true,
                confirmButtonColor: '#dc3545',
                cancelButtonColor: '#6c757d',
                confirmButtonText: '<i class="bi bi-check2 me-1"></i> Sí, continuar',
                cancelButtonText: '<i class="bi bi-x-circle me-1"></i> Cancelar',
                reverseButtons: true
            }).then(function (result) {
                if (result.isConfirmed && typeof callback === 'function') {
                    callback();
                }
            });
        } else {
            if (confirm(pregunta + '\n' + detalle) && typeof callback === 'function') {
                callback();
            }
        }
    };

    // --- 1.5: BASE DE CONOCIMIENTO Y CENTRO DE AYUDA CONTEXTUAL ---
    PAE.guiasAyuda = {
        // ADMIN
        'admin-matricula': {
            titulo: 'Gestión de Matrículas',
            icono: 'bi-clipboard-check',
            rol: 'Administrador',
            queHace: 'Permite consultar el estado de las matrículas estudiantiles, filtrar alumnos por grado escolar (6° a 11°) y actualizar su estado a Activa o Finalizada.',
            pasos: [
                'Usa los filtros superiores para seleccionar el grado y el estado deseado.',
                'En la columna de acciones, haz clic en «Editar» para cambiar el grado o estado del estudiante.',
                'Guarda los cambios haciendo clic en «Guardar Cambios» para actualizar el registro en la base de datos.'
            ],
            consejos: 'Las matrículas activas permiten que el estudiante aparezca en las planillas de los docentes y en la generación de certificados.'
        },
        'admin-usuarios': {
            titulo: 'Gestión de Usuarios',
            icono: 'bi-people-fill',
            rol: 'Administrador',
            queHace: 'Administra todas las cuentas de acceso del portal (Estudiantes, Docentes y Acudientes), permitiendo registrar nuevos usuarios, buscar por documento o nombre y actualizar roles.',
            pasos: [
                'Para registrar un nuevo usuario, presiona el botón «Registrar Usuario» arriba a la derecha.',
                'Diligencia el formulario con datos personales, rol correspondiente y contraseña temporal.',
                'Si el usuario es estudiante, puedes vincularlo inmediatamente a sus acudientes responsables.'
            ],
            consejos: 'Verifica siempre que el número de identificación y correo institucional no se encuentren duplicados.'
        },
        'admin-certificados': {
            titulo: 'Generación de Certificados',
            icono: 'bi-file-earmark-pdf-fill',
            rol: 'Administrador',
            queHace: 'Emite certificados oficiales de estudio y constancias de matrícula en formato PDF de alta calidad con código de verificación QR seguro.',
            pasos: [
                'Busca al estudiante por nombre o documento en la lista.',
                'Selecciona el tipo de certificado que necesitas expedir.',
                'Presiona «Generar Certificado» para previsualizar y descargar el documento PDF firmado digitalmente.'
            ],
            consejos: 'Cualquier persona puede validar la autenticidad del certificado escaneando el código QR impreso en el documento.'
        },
        'admin-notificaciones': {
            titulo: 'Comunicados y Notificaciones',
            icono: 'bi-megaphone-fill',
            rol: 'Administrador',
            queHace: 'Publica anuncios institucionales dirigidos a toda la comunidad educativa o filtrados exclusivamente para estudiantes, profesores o acudientes.',
            pasos: [
                'Usa el desplegable de «Plantillas Rápidas» para elegir un título y borrador predeterminado.',
                'Selecciona el rol de destino (Todos, Estudiantes, Docentes o Acudientes).',
                'Define la prioridad (Leve o Alta) y fecha de expiración si el aviso tiene límite de tiempo.',
                'Presiona «Publicar Comunicado» para enviarlo instantáneamente a los destinatarios.'
            ],
            consejos: 'Los comunicados de prioridad alta no pueden ser descartados por los usuarios, asegurando su lectura obligatoria.'
        },
        'admin-asignacion': {
            titulo: 'Asignación Académica',
            icono: 'bi-diagram-3-fill',
            rol: 'Administrador',
            queHace: 'Permite distribuir la carga docente asignando materias, grupos y horarios semanales a cada profesor.',
            pasos: [
                'Selecciona el docente en el menú desplegable.',
                'Escoge la materia y el grado correspondiente.',
                'Asigna los bloques horarios y presiona «Guardar Asignación».'
            ],
            consejos: 'Asegúrate de que no haya cruces de horario para el mismo docente en el mismo bloque académico.'
        },
        'admin-reportes': {
            titulo: 'Reportes Pro Consolidados',
            icono: 'bi-graph-up-arrow',
            rol: 'Administrador',
            queHace: 'Genera estadísticas consolidadas anuales, índices de rendimiento escolar y métricas generales de matrícula de la institución.',
            pasos: [
                'Selecciona el periodo académico a consultar.',
                'Revisa los gráficos de rendimiento y aprobación.',
                'Haz clic en «Exportar Reporte» para obtener el consolidado en Excel o PDF.'
            ],
            consejos: 'Ideal para informes de gestión ante el comité académico y rectoría.'
        },

        // DOCENTE
        'docente-planilla': {
            titulo: 'Planilla de Calificaciones',
            icono: 'bi-table',
            rol: 'Docente',
            queHace: 'Módulo principal del docente para ingresar, editar y calcular notas parciales, definitivas y observaciones de sus materias asignadas.',
            pasos: [
                'Selecciona la materia, grado y periodo académico en los filtros superiores.',
                'Digita las calificaciones en la escala institucional (1.0 a 5.0). El sistema calcula el promedio automáticamente.',
                'Haz clic en el botón verde «Guardar Calificaciones» para registrar las notas de forma segura en la base de datos.'
            ],
            consejos: 'El sistema valida automáticamente que no ingreses notas fuera de la escala (1.0 - 5.0).'
        },
        'docente-asistencia': {
            titulo: 'Control de Asistencia',
            icono: 'bi-calendar-check-fill',
            rol: 'Docente',
            queHace: 'Registro diario de asistencia, tardanzas y ausencias justificadas o injustificadas de los estudiantes.',
            pasos: [
                'Elige la fecha actual y el grupo escolar.',
                'Marca los estados correspondientes (Presente, Ausente, Retardo, Excusa).',
                'Presiona «Guardar Asistencia» para enviar la notificación a los acudientes.'
            ],
            consejos: 'Los acudientes pueden ver en tiempo real si su hijo presentó alguna inasistencia el día de hoy.'
        },
        'docente-observaciones': {
            titulo: 'Observador del Alumno',
            icono: 'bi-journal-bookmark-fill',
            rol: 'Docente',
            queHace: 'Permite registrar anotaciones pedagógicas, llamados de atención formativos o felicitaciones en la hoja de vida del estudiante.',
            pasos: [
                'Busca al estudiante en la lista de su grado.',
                'Redacta la observación describiendo el hecho pedagógico de forma respetuosa y clara.',
                'Selecciona el tipo de anotación (Formativa, Positiva o Disciplinaria) y guarda.'
            ],
            consejos: 'El observador digital queda archivado en el expediente del estudiante para el seguimiento de convivencia.'
        },
        'docente-actividades': {
            titulo: 'Actividades y Aula Virtual (Classroom)',
            icono: 'bi-easel2-fill',
            rol: 'Docente',
            queHace: 'Crea tareas, talleres y guías de trabajo con fecha de entrega, adjuntos y recepción de evidencias enviadas por los estudiantes.',
            pasos: [
                'Presiona «Nueva Actividad» para crear una tarea para tu grupo.',
                'Ingresa el título, descripción, fecha límite y adjunta documentos si aplica.',
                'Revisa los avances enviados por tus estudiantes y asígnales una retroalimentación.'
            ],
            consejos: 'Los estudiantes recibirán una alerta automática al publicarse una nueva actividad.'
        },
        'docente-mejoras': {
            titulo: 'Planes de Apoyo y Nivelación',
            icono: 'bi-arrow-up-right-circle-fill',
            rol: 'Docente',
            queHace: 'Asigna talleres de recuperación y planes de mejoramiento para estudiantes con desempeño bajo al finalizar el periodo.',
            pasos: [
                'Filtra a los estudiantes que perdieron la materia.',
                'Asigna la guía pedagógica de nivelación y fecha de sustentación.',
                'Registra la nota de recuperación una vez sustentada la actividad.'
            ],
            consejos: 'Cumple con los lineamientos del SIEE para brindar oportunidades de superación pedagógica.'
        },
        'docente-cursosextra': {
            titulo: 'Cursos Extracurriculares',
            icono: 'bi-stars',
            rol: 'Docente',
            queHace: 'Gestiona clubes de robótica, danzas, artes, deportes y música extracurriculares ofrecidos en la jornada complementaria.',
            pasos: [
                'Consulta la lista de inscritos a tus cursos extra.',
                'Publica novedades, avisos de ensayos y torneos institucionales.'
            ],
            consejos: 'Fomenta la participación activa de los estudiantes en actividades integrales.'
        },

        // ESTUDIANTE
        'estudiante-calificaciones': {
            titulo: 'Mis Calificaciones',
            icono: 'bi-award-fill',
            rol: 'Estudiante',
            queHace: 'Visualiza tus notas parciales, promedio acumulado y retroalimentación de los docentes en cada una de las asignaturas.',
            pasos: [
                'Selecciona el periodo que deseas consultar.',
                'Revisa el listado de materias con su nota definitiva actual y estado (Aprobado / Pendiente).',
                'Haz clic en cualquier materia para ver el desglose de talleres, evaluaciones y autoevaluación.'
            ],
            consejos: 'Si notas alguna calificación errónea, acércate respetuosamente con tu docente antes del cierre del periodo.'
        },
        'estudiante-actividades': {
            titulo: 'Mis Tareas y Actividades',
            icono: 'bi-journal-check',
            rol: 'Estudiante',
            queHace: 'Consulta las tareas asignadas por tus profesores, fechas límites de entrega y sube tus evidencias o documentos resueltos.',
            pasos: [
                'Revisa las tareas pendientes en orden de entrega.',
                'Lee las instrucciones del profesor y descarga los archivos adjuntos.',
                'Adjunta tu archivo de respuesta (PDF, imagen o texto) y presiona «Enviar Avance».'
            ],
            consejos: 'Envía tus tareas con suficiente tiempo para evitar problemas con la fecha límite.'
        },
        'estudiante-cursosextra': {
            titulo: 'Mis Cursos Extracurriculares',
            icono: 'bi-star-fill',
            rol: 'Estudiante',
            queHace: 'Consulta tus talleres de artes, deportes o clubes en los que estás matriculado.',
            pasos: [
                'Consulta los horarios y salones asignados para tus clubes extracurriculares.'
            ],
            consejos: 'La constancia en los cursos complementarios enriquece tu formación personal.'
        },
        'estudiante-mejoras': {
            titulo: 'Planes de Nivelación',
            icono: 'bi-shield-check',
            rol: 'Estudiante',
            queHace: 'Muestra las actividades de recuperación asignadas en materias donde necesitas nivelar notas.',
            pasos: [
                'Descarga el taller de recuperación.',
                'Prepárate para la sustentación en la fecha indicada por tu profesor.'
            ],
            consejos: 'Aprovecha las oportunidades de nivelación para mantener tu boletín al día.'
        },

        // ACUDIENTE
        'acudiente-calificaciones': {
            titulo: 'Calificaciones de mi Hijo/a',
            icono: 'bi-card-checklist',
            rol: 'Acudiente',
            queHace: 'Permite a los padres de familia monitorear el desempeño académico, notas y boletines de sus hijos en tiempo real.',
            pasos: [
                'Si tienes más de un hijo matriculado, elígelo en el selector superior.',
                'Consulta las asignaturas y su calificación actual.'
            ],
            consejos: 'Acompaña a tu hijo revisando sus materias semanalmente para apoyar su proceso educativo.'
        },
        'acudiente-asistencia': {
            titulo: 'Registro de Asistencia del Estudiante',
            icono: 'bi-clock-history',
            rol: 'Acudiente',
            queHace: 'Notifica y registra las inasistencias o retrasos del estudiante a la institución.',
            pasos: [
                'Revisa el calendario con las fechas de faltas reportadas.',
                'Si tienes justificación médica, acércala a coordinación académica.'
            ],
            consejos: 'La puntualidad y asistencia constante son esenciales para el rendimiento escolar.'
        },
        'acudiente-citaciones': {
            titulo: 'Citaciones y Circulares',
            icono: 'bi-envelope-paper-fill',
            rol: 'Acudiente',
            queHace: 'Recibe convocatorias a reuniones de padres de familia, entrega de boletines o citaciones individuales de docentes.',
            pasos: [
                'Revisa las citaciones programadas con fecha y hora.',
                'Confirma tu asistencia con puntualidad.'
            ],
            consejos: 'Mantente informado revisando tus comunicados con frecuencia.'
        },
        'acudiente-observador': {
            titulo: 'Observador de Convivencia',
            icono: 'bi-file-person-fill',
            rol: 'Acudiente',
            queHace: 'Permite conocer los compromisos pedagógicos, llamados de atención o felicitaciones emitidas por la institución hacia tu hijo.',
            pasos: [
                'Consulta las anotaciones emitidas por los directores de grupo y docentes.',
                'Dialoga en familia sobre las observaciones para fortalecer los valores y la disciplina.'
            ],
            consejos: 'Una comunicación cercana entre colegio y familia garantiza el éxito formativo.'
        }
    };

    // Función para abrir la ayuda con contexto
    PAE.abrirAyuda = function (claveModulo) {
        // Si no se pasa clave, intentar autodetectar por submódulo visible
        if (!claveModulo) {
            const subActivo = document.querySelector('.submodulo-activo, [id$="-matricula"]:not(.d-none), [id$="-planilla"]:not(.d-none), [id$="-asistencia"]:not(.d-none), [id$="-certificados"]:not(.d-none), [id$="-reportes"]:not(.d-none), [id$="-calificaciones"]:not(.d-none), [id$="-actividades"]:not(.d-none)');
            if (subActivo) {
                claveModulo = subActivo.id;
            } else {
                // Detectar por URL
                const path = window.location.pathname.toLowerCase();
                if (path.includes('notificaciones')) claveModulo = 'admin-notificaciones';
                else if (path.includes('usuarios')) claveModulo = 'admin-usuarios';
                else if (path.includes('asignacion')) claveModulo = 'admin-asignacion';
                else if (path.includes('misnotas')) claveModulo = 'estudiante-calificaciones';
                else claveModulo = 'admin-matricula';
            }
        }

        const guia = PAE.guiasAyuda[claveModulo] || PAE.guiasAyuda['admin-matricula'];

        // Poblar el modal
        const lblTitulo = document.getElementById('ayudaModalTitulo');
        const lblRol = document.getElementById('ayudaModalRol');
        const lblQueHace = document.getElementById('ayudaModalQueHace');
        const listaPasos = document.getElementById('ayudaModalPasos');
        const lblConsejos = document.getElementById('ayudaModalConsejo');

        if (lblTitulo) lblTitulo.innerHTML = `<i class="bi ${guia.icono} me-2" style="color: #e67e22;"></i> ${guia.titulo}`;
        if (lblRol) lblRol.textContent = guia.rol;
        if (lblQueHace) lblQueHace.textContent = guia.queHace;

        if (listaPasos) {
            listaPasos.innerHTML = '';
            guia.pasos.forEach((p, idx) => {
                const li = document.createElement('li');
                li.className = 'mb-2 d-flex align-items-start';
                li.innerHTML = `<span class="badge rounded-pill me-2 text-white" style="background-color: #0d2a4a; min-width: 24px;">${idx + 1}</span> <span>${p}</span>`;
                listaPasos.appendChild(li);
            });
        }

        if (lblConsejos) lblConsejos.textContent = guia.consejos;

        // Abrir modal de Bootstrap
        const modalEl = document.getElementById('modalAyudaPAE');
        if (modalEl && typeof bootstrap !== 'undefined') {
            const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            modal.show();
        }
    };
})();

// Atajos globales
function abrirModalAyudaGlobal() {
    PAE.abrirAyuda();
}

function abrirModalAyudaContextual(clave) {
    PAE.abrirAyuda(clave);
}
