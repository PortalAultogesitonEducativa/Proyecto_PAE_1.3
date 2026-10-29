// ==========================================================================
// PAE - PORTAL DE AUTOGESTIÓN EDUCATIVA
// NÚCLEO DE COMUNICACIÓN, ALERTAS AMIGABLES, ACCESIBILIDAD Y SISTEMA DE AYUDA
// ==========================================================================

window.PAE = window.PAE || {};

(function () {
    // --- ACCESIBILIDAD: ANUNCIADOR LIVE REGION PARA LECTORES DE PANTALLA ---
    function anunciarAccesible(texto) {
        if (!texto) return;
        let announcer = document.getElementById('pae-live-announcer');
        if (!announcer) {
            announcer = document.createElement('div');
            announcer.id = 'pae-live-announcer';
            announcer.setAttribute('aria-live', 'polite');
            announcer.setAttribute('aria-atomic', 'true');
            announcer.className = 'visually-hidden';
            document.body.appendChild(announcer);
        }
        announcer.textContent = '';
        setTimeout(() => {
            announcer.textContent = texto;
        }, 50);
    }

    // --- COMUNICACIÓN AMIGABLE Y NOTIFICACIONES ESTANDARIZADAS ---

    // Notificación flotante (Toast)
    PAE.toast = function (mensaje, tipo) {
        mensaje = mensaje || 'Operación completada.';
        tipo = tipo || 'success';
        const icono = (tipo === 'error') ? 'error' : (tipo === 'warning') ? 'warning' : (tipo === 'info') ? 'info' : 'success';

        anunciarAccesible(mensaje);

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
        mensaje = mensaje || 'La operación se completó correctamente.';
        titulo = titulo || '¡Operación Exitosa!';

        anunciarAccesible(titulo + ': ' + mensaje);

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
        // Sanitizar posibles mensajes técnicos o excepciones internas
        let mensajeLimpio = mensaje;
        if (!mensajeLimpio || mensajeLimpio.includes('Exception') || mensajeLimpio.includes('Sql') || mensajeLimpio.includes('Stack') || mensajeLimpio.includes('SELECT') || mensajeLimpio.includes('INSERT')) {
            console.error('[PAE Internal Error]:', mensaje);
            mensajeLimpio = 'No fue posible completar la acción solicitada. Verifique los datos e inténtelo nuevamente.';
        }
        titulo = titulo || 'Atención';

        anunciarAccesible(titulo + ': ' + mensajeLimpio);

        if (typeof Swal !== 'undefined') {
            return Swal.fire({
                icon: 'error',
                title: titulo,
                text: mensajeLimpio,
                confirmButtonColor: '#0d2a4a',
                confirmButtonText: '<i class="bi bi-arrow-repeat me-1"></i> Entendido'
            });
        } else {
            alert(mensajeLimpio);
        }
    };

    // Indicador accesible de carga / operación en curso
    PAE.cargando = function (mensaje) {
        mensaje = mensaje || 'Guardando los cambios... Por favor espere.';
        anunciarAccesible(mensaje);

        if (typeof Swal !== 'undefined') {
            Swal.fire({
                title: mensaje,
                allowOutsideClick: false,
                allowEscapeKey: false,
                showConfirmButton: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });
        }
    };

    PAE.cerrarCargando = function () {
        if (typeof Swal !== 'undefined' && Swal.isVisible()) {
            Swal.close();
        }
    };

        // Confirmación amigable polimórfica (soporta firmas con detalle o callback directo)
    PAE.confirmar = function (pregunta, detalleOCallback, callbackOTitulo, textoConfirmar, colorConfirmar) {
        pregunta = pregunta || '¿Está seguro de continuar?';
        let callback = null;
        let detalle = '';
        let confirmText = '<i class="bi bi-check-circle-fill me-1"></i> Sí, continuar';
        let confirmColor = '#6f42c1';

        if (typeof detalleOCallback === 'function') {
            // Firma: PAE.confirmar(pregunta, callback, tituloODetalle, textoConfirmar, colorConfirmar)
            callback = detalleOCallback;
            detalle = (typeof callbackOTitulo === 'string') ? callbackOTitulo : '';
            if (typeof textoConfirmar === 'string' && textoConfirmar.trim()) {
                confirmText = `<i class="bi bi-check-lg me-1"></i> ${textoConfirmar}`;
            }
            if (colorConfirmar) {
                confirmColor = colorConfirmar;
            }
        } else {
            // Firma: PAE.confirmar(pregunta, detalle, callback)
            detalle = detalleOCallback || '';
            callback = callbackOTitulo;
            if (typeof textoConfirmar === 'string' && textoConfirmar.trim()) {
                confirmText = `<i class="bi bi-check-lg me-1"></i> ${textoConfirmar}`;
            }
        }

        const textoCompleto = (pregunta + ' ' + detalle).toLowerCase();
        if (textoCompleto.includes('eliminar') || textoCompleto.includes('rechazar')) {
            confirmColor = confirmColor === '#6f42c1' ? '#dc3545' : confirmColor;
            if (confirmText.includes('Sí, continuar')) {
                confirmText = '<i class="bi bi-trash-fill me-1"></i> Sí, confirmar';
            }
        }

        if (typeof anunciarAccesible === 'function') {
            anunciarAccesible(pregunta);
        }

        if (typeof Swal !== 'undefined') {
            return Swal.fire({
                icon: (textoCompleto.includes('eliminar') || textoCompleto.includes('rechazar')) ? 'warning' : 'question',
                title: pregunta,
                text: detalle || undefined,
                showCancelButton: true,
                confirmButtonColor: confirmColor,
                cancelButtonColor: '#6c757d',
                confirmButtonText: confirmText,
                cancelButtonText: '<i class="bi bi-x-circle me-1"></i> Cancelar',
                reverseButtons: true
            }).then(function (result) {
                if (result.isConfirmed && typeof callback === 'function') {
                    callback();
                }
            });
        } else {
            if (confirm(pregunta + (detalle ? '\n' + detalle : '')) && typeof callback === 'function') {
                callback();
            }
        }
    };

    // Notificaciones estándar PAE
    PAE.notificarExito = function (mensaje) {
        if (typeof PAE.toast === 'function') {
            PAE.toast(mensaje, 'success');
        } else if (typeof PAE.exito === 'function') {
            PAE.exito(mensaje);
        } else {
            alert(mensaje);
        }
    };

    PAE.notificarError = function (mensaje) {
        if (typeof PAE.error === 'function') {
            PAE.error(mensaje);
        } else if (typeof PAE.toast === 'function') {
            PAE.toast(mensaje, 'error');
        } else {
            alert(mensaje);
        }
    };

    // --- BASE DE CONOCIMIENTO Y CENTRO DE AYUDA CONTEXTUAL ---
    PAE.guiasAyuda = {
        // PQR
        'admin-pqr': {
            titulo: 'Gestión y Respuesta de PQR',
            icono: 'bi-chat-heart-fill',
            rol: 'Administrador / Coordinador',
            queHace: 'Permite gestionar de manera centralizada todas las Peticiones, Quejas y Reclamos radicadas por los miembros de la institución educativa.',
            pasos: [
                'Revisa el listado de solicitudes recibidas clasificadas por fecha y estado (Recibida, En revisión, Respondida, Cerrada).',
                'Haz clic en "Detalle" para consultar los antecedentes y hechos reportados por el usuario solicitante.',
                'Presiona "Gestionar" para redactar la respuesta formal institucional y cambiar el estado del trámite.'
            ],
            consejos: 'Las respuestas quedan registradas con firma digital, fecha y usuario responsable para garantizar trazabilidad.',
            queHacerSiNoSabe: 'Si una solicitud requiere consulta previa con directivas o comités académicos, coloca su estado en "En revisión" mientras se reúne el equipo.'
        },
        'estudiante-pqr': {
            titulo: 'Radicación y Seguimiento de PQR',
            icono: 'bi-chat-heart-fill',
            rol: 'Comunidad Educativa',
            queHace: 'Facilita a estudiantes, docentes y acudientes la radicación de solicitudes formales, sugerencias o quejas ante la institución.',
            pasos: [
                'Haz clic en el botón amarillo "Radicar PQR".',
                'Selecciona el tipo de requerimiento: Petición (trámite o información), Queja (inconformidad con el servicio) o Reclamo (vulneración o exigencia de un derecho).',
                'Escribe un asunto claro y describe en detalle tu situación.',
                'Presiona "Radicar Solicitud" para obtener tu código único de radicado (Ej: PQR-2026-0001) con el que podrás consultar la respuesta.'
            ],
            consejos: 'Verifica tu número de radicado; cuando la administración responda podrás leer el dictamen en el botón "Detalle".',
            queHacerSiNoSabe: 'Si tienes dudas sobre qué tipo de solicitud elegir, selecciona "Petición". La administración la evaluará con la misma atención.'
        },

        // ADMIN
        'admin-matricula': {
            titulo: 'Gestión de Matrículas',
            icono: 'bi-clipboard-check',
            rol: 'Administrador',
            queHace: 'Permite consultar el estado de las matrículas estudiantiles, filtrar alumnos por grado escolar (6° a 11°) y actualizar su estado a Activa o Finalizada.',
            pasos: [
                'Usa los filtros superiores para seleccionar el grado y el estado deseado.',
                'En la columna de acciones, haz clic en "Editar" para cambiar el grado o estado del estudiante.',
                'Guarda los cambios haciendo clic en "Guardar Cambios" para actualizar el registro en la base de datos.'
            ],
            consejos: 'Las matrículas activas permiten que el estudiante aparezca en las planillas de los docentes y en la generación de certificados.',
            queHacerSiNoSabe: 'Si un alumno se retira temporalmente, cámbialo a estado "Finalizada" en vez de eliminar su historial.'
        },
        'admin-usuarios': {
            titulo: 'Gestión de Usuarios',
            icono: 'bi-people-fill',
            rol: 'Administrador',
            queHace: 'Administra todas las cuentas de acceso del portal (Estudiantes, Docentes y Acudientes), permitiendo registrar nuevos usuarios, buscar por documento o nombre y actualizar roles.',
            pasos: [
                'Para registrar un nuevo usuario, presiona el botón "Registrar Usuario" arriba a la derecha.',
                'Diligencia el formulario con datos personales, rol correspondiente y contraseña temporal.',
                'Si el usuario es estudiante, puedes vincularlo inmediatamente a sus acudientes responsables.'
            ],
            consejos: 'Verifica siempre que el número de identificación y correo institucional no se encuentren duplicados.',
            queHacerSiNoSabe: 'Si un usuario olvida su clave, puedes restablecer su acceso o indicarle que use la opción "Olvidé mi contraseña" en la pantalla de inicio.'
        },
        'admin-certificados': {
            titulo: 'Generación de Certificados',
            icono: 'bi-file-earmark-pdf-fill',
            rol: 'Administrador',
            queHace: 'Emite certificados oficiales de estudio y constancias de matrícula en formato PDF de alta calidad con código de verificación QR seguro.',
            pasos: [
                'Busca al estudiante por nombre o documento en la lista.',
                'Selecciona el tipo de certificado que necesitas expedir.',
                'Presiona "Generar Certificado" para previsualizar y descargar el documento PDF firmado digitalmente.'
            ],
            consejos: 'Cualquier persona puede validar la autenticidad del certificado escaneando el código QR impreso en el documento.',
            queHacerSiNoSabe: 'Si no encuentras al estudiante, verifica que se encuentre con matrícula "Activa" en el módulo de Matrículas.'
        },
        'admin-notificaciones': {
            titulo: 'Comunicados y Notificaciones',
            icono: 'bi-megaphone-fill',
            rol: 'Administrador',
            queHace: 'Publica anuncios institucionales dirigidos a toda la comunidad educativa o filtrados exclusivamente para estudiantes, profesores o acudientes.',
            pasos: [
                'Usa el desplegable de "Plantillas Rápidas" para elegir un título y borrador predeterminado.',
                'Selecciona el rol de destino (Todos, Estudiantes, Docentes o Acudientes).',
                'Define la prioridad (Leve o Alta) y fecha de expiración si el aviso tiene límite de tiempo.',
                'Presiona "Publicar Comunicado" para enviarlo instantáneamente a los destinatarios.'
            ],
            consejos: 'Los comunicados de prioridad alta no pueden ser descartados por los usuarios, asegurando su lectura obligatoria.',
            queHacerSiNoSabe: 'Para comunicados generales a padres de familia y alumnos, selecciona la opción "Todos".'
        },
        'admin-asignacion': {
            titulo: 'Asignación Académica',
            icono: 'bi-diagram-3-fill',
            rol: 'Administrador / Coordinación',
            queHace: 'Permite distribuir la carga docente asignando materias, grupos y horarios semanales a cada profesor.',
            pasos: [
                'Selecciona el docente en el menú desplegable.',
                'Escoge la materia y el grado correspondiente.',
                'Asigna los bloques horarios y presiona "Guardar Asignación".'
            ],
            consejos: 'Asegúrate de que no haya cruces de horario para el mismo docente en el mismo bloque académico.',
            queHacerSiNoSabe: 'Puedes consultar el horario global docente para verificar qué horas tiene libres cada profesor.'
        },
        'admin-reportes': {
            titulo: 'Reportes Pro Consolidados',
            icono: 'bi-graph-up-arrow',
            rol: 'Administrador',
            queHace: 'Genera estadísticas consolidadas anuales, índices de rendimiento escolar y métricas generales de matrícula de la institución.',
            pasos: [
                'Selecciona el periodo académico a consultar.',
                'Revisa los gráficos de rendimiento y aprobación.',
                'Haz clic en "Exportar Reporte" para obtener el consolidado en Excel o PDF.'
            ],
            consejos: 'Ideal para informes de gestión ante el comité académico y rectoría.',
            queHacerSiNoSabe: 'Si los promedios están en cero, recuerda solicitar a los docentes que digiten notas definitivas en sus planillas.'
        },

        // DOCENTE
        'docente-planilla': {
            titulo: 'Planilla de Calificaciones',
            icono: 'bi-table',
            rol: 'Docente',
            queHace: 'Permite calificar a los alumnos por asignatura y periodo, promediando automáticamente talleres, evaluaciones y autoevaluación.',
            pasos: [
                'Selecciona el curso (grado) y la asignatura que deseas evaluar.',
                'Digita las notas en escala de 1.0 a 5.0 en cada columna.',
                'El sistema calcula automáticamente la definitiva en color verde (aprobada) o rojo (reprobada).',
                'Haz clic en "Guardar Calificaciones" para registrar las notas.'
            ],
            consejos: 'Guarda periódicamente para evitar pérdida de datos si tu conexión a internet es inestable.',
            queHacerSiNoSabe: 'Si falta algún alumno en tu lista, contacta al administrador para que verifique la matrícula del estudiante.'
        },
        'docente-asistencia': {
            titulo: 'Control Diario de Asistencia',
            icono: 'bi-calendar-check-fill',
            rol: 'Docente',
            queHace: 'Registra la presencia, faltas justificadas/injustificadas y retardos de los estudiantes hora a hora.',
            pasos: [
                'Selecciona la fecha y el bloque de clase.',
                'Marca P (Presente), F (Falla) o R (Retardo) para cada estudiante.',
                'Anota observaciones de convivencia si corresponde y presiona "Guardar Asistencia".'
            ],
            consejos: 'Las fallas registradas se notifican inmediatamente al portal de los acudientes.',
            queHacerSiNoSabe: 'Puedes usar el botón "Marcar Todos Presentes" para agilizar el llamado a lista diario.'
        },
        'docente-observaciones': {
            titulo: 'Observador del Alumno',
            icono: 'bi-journal-bookmark-fill',
            rol: 'Docente',
            queHace: 'Anotador oficial del seguimiento formativo, llamados de atención disciplinarios, actas de compromiso y felicitaciones.',
            pasos: [
                'Busca al estudiante y selecciona el tipo de anotación.',
                'Describe detalladamente los hechos ocurridos y los compromisos adquiridos.',
                'Guarda la anotación para que quede en el historial oficial del estudiante.'
            ],
            consejos: 'Mantén una redacción respetuosa y formativa centrada en el crecimiento pedagógico del alumno.',
            queHacerSiNoSabe: 'En casos graves de convivencia, notifica también a coordinación académica o de convivencia.'
        },
        'docente-actividades': {
            titulo: 'Aula Virtual y Classroom',
            icono: 'bi-easel2-fill',
            rol: 'Docente',
            queHace: 'Crea talleres, tareas y guías de clase con fecha límite de entrega para que los estudiantes suban sus evidencias.',
            pasos: [
                'Elige el curso y haz clic en "Crear Nueva Tarea".',
                'Define el título, fecha de entrega y adjunta archivos PDF o guías.',
                'Revisa y califica los archivos entregados por los estudiantes desde la pestaña de entregas.'
            ],
            consejos: 'Establece instrucciones claras para que los estudiantes sepan qué formato deben entregar.',
            queHacerSiNoSabe: 'Si los alumnos no pueden ver la tarea, confirma que el grado seleccionado coincida con el curso de los alumnos.'
        },
        'docente-mejoras': {
            titulo: 'Planes de Mejoramiento y Nivelación',
            icono: 'bi-arrow-up-right-circle-fill',
            rol: 'Docente',
            queHace: 'Asigna talleres de recuperación y planes de mejoramiento para estudiantes con desempeño bajo al finalizar el periodo.',
            pasos: [
                'Filtra a los estudiantes que perdieron la materia.',
                'Asigna la guía pedagógica de nivelación y fecha de sustentación.',
                'Registra la nota de recuperación una vez sustentada la actividad.'
            ],
            consejos: 'Cumple con los lineamientos del SIEE para brindar oportunidades de superación pedagógica.',
            queHacerSiNoSabe: 'Puedes consultar los antecedentes de notas del estudiante antes de fijar los puntos del taller.'
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
            consejos: 'Fomenta la participación activa de los estudiantes en actividades integrales.',
            queHacerSiNoSabe: 'Para abrir un nuevo club extracurricular, coordina con la dirección académica del colegio.'
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
            consejos: 'Si notas alguna calificación errónea, acércate respetuosamente con tu docente antes del cierre del periodo.',
            queHacerSiNoSabe: 'Si una materia tiene calificación pendiente, pregunta a tu docente la fecha de publicación de notas.'
        },
        'estudiante-actividades': {
            titulo: 'Mis Tareas y Actividades',
            icono: 'bi-journal-check',
            rol: 'Estudiante',
            queHace: 'Consulta las tareas asignadas por tus profesores, fechas límites de entrega y sube tus evidencias o documentos resueltos.',
            pasos: [
                'Revisa las tareas pendientes en orden de entrega.',
                'Lee las instrucciones del profesor y descarga los archivos adjuntos.',
                'Adjunta tu archivo de respuesta (PDF, imagen o texto) y presiona "Enviar Avance".'
            ],
            consejos: 'Envía tus tareas con suficiente tiempo para evitar problemas con la fecha límite.',
            queHacerSiNoSabe: 'Si la fecha límite ya venció, solicita autorización al profesor antes de radicar fuera de plazo.'
        },
        'estudiante-cursosextra': {
            titulo: 'Mis Cursos Extracurriculares',
            icono: 'bi-star-fill',
            rol: 'Estudiante',
            queHace: 'Consulta tus talleres de artes, deportes o clubes en los que estás matriculado.',
            pasos: [
                'Consulta los horarios y salones asignados para tus clubes extracurriculares.'
            ],
            consejos: 'La constancia en los cursos complementarios enriquece tu formación personal.',
            queHacerSiNoSabe: 'Para cambiarte de curso extracurricular, solicita apoyo en coordinación estudiantil.'
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
            consejos: 'Aprovecha las oportunidades de nivelación para mantener tu boletín al día.',
            queHacerSiNoSabe: 'Si tienes dudas sobre un ejercicio del taller, solicita una asesoría previa a tu profesor.'
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
            consejos: 'Acompaña a tu hijo revisando sus materias semanalmente para apoyar su proceso educativo.',
            queHacerSiNoSabe: 'Si notas calificaciones bajas repetidas, solicita una cita de atención a padres con el docente.'
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
            consejos: 'La puntualidad y asistencia constante son esenciales para el rendimiento escolar.',
            queHacerSiNoSabe: 'Para justificar una ausencia justificada, presenta la constancia física o radica una PQR con soporte.'
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
            consejos: 'Mantente informado revisando tus comunicados con frecuencia.',
            queHacerSiNoSabe: 'Si no puedes asistir a una reunión, informa con antelación al director de grupo.'
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
            consejos: 'Una comunicación cercana entre colegio y familia garantiza el éxito formativo.',
            queHacerSiNoSabe: 'Firma y atiende oportunamente los compromisos escolares concertados con los docentes.'
        }
    };

    // Función para abrir la ayuda con contexto
    PAE.abrirAyuda = function (claveModulo) {
        // Si no se pasa clave, intentar autodetectar por submódulo visible o URL
        if (!claveModulo) {
            const subActivo = document.querySelector('.submodulo-activo, [id$="-matricula"]:not(.d-none), [id$="-planilla"]:not(.d-none), [id$="-asistencia"]:not(.d-none), [id$="-certificados"]:not(.d-none), [id$="-reportes"]:not(.d-none), [id$="-calificaciones"]:not(.d-none), [id$="-actividades"]:not(.d-none)');
            if (subActivo && subActivo.id) {
                claveModulo = subActivo.id;
            } else {
                const path = window.location.pathname.toLowerCase();
                if (path.includes('pqr')) claveModulo = 'admin-pqr';
                else if (path.includes('notificaciones')) claveModulo = 'admin-notificaciones';
                else if (path.includes('usuarios')) claveModulo = 'admin-usuarios';
                else if (path.includes('asignacion')) claveModulo = 'admin-asignacion';
                else if (path.includes('planilla')) claveModulo = 'docente-planilla';
                else if (path.includes('misnotas')) claveModulo = 'estudiante-calificaciones';
                else if (path.includes('supervision')) claveModulo = 'admin-asignacion';
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
        const lblQueHacerSiNoSabe = document.getElementById('ayudaModalQueHacerSiNoSabe');

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

        if (lblQueHacerSiNoSabe) {
            lblQueHacerSiNoSabe.textContent = guia.queHacerSiNoSabe || 'Si tienes dudas o no sabes cómo continuar, puedes usar el botón de volver al menú de inicio o radicar una solicitud en el Módulo PQR.';
        }

        // Abrir modal de Bootstrap
        const modalEl = document.getElementById('modalAyudaPAE');
        if (modalEl && typeof bootstrap !== 'undefined') {
            const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            modal.show();
        }
    };

    // Prevenir dobles envíos accidentales en formularios estándar
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('form').forEach(form => {
            if (form.classList.contains('pae-no-double-submit-prevent')) return;
            form.addEventListener('submit', function (e) {
                if (form.getAttribute('data-submitting') === 'true') {
                    e.preventDefault();
                    return false;
                }
                const btnSubmit = form.querySelector('button[type="submit"], input[type="submit"]');
                if (btnSubmit && !btnSubmit.disabled && !form.checkValidity || (form.checkValidity && form.checkValidity())) {
                    form.setAttribute('data-submitting', 'true');
                    setTimeout(() => {
                        form.removeAttribute('data-submitting');
                    }, 5000);
                }
            });
        });
    });
})();

// Atajos globales
function abrirModalAyudaGlobal() {
    PAE.abrirAyuda();
}

function abrirModalAyudaContextual(clave) {
    PAE.abrirAyuda(clave);
}
