package application

import actors._
import akka.actor.typed.{ActorSystem, Behavior, PostStop}
import akka.actor.typed.scaladsl.Behaviors
import akka.http.scaladsl.Http
import akka.http.scaladsl.Http.ServerBinding
import akka.http.scaladsl.server.Directives._

import scala.util.{Failure, Success}
import scala.concurrent.Future
import application.endpoints._
import application.handlers.SessionHandlers
import sttp.tapir._
import sttp.tapir.docs.openapi.OpenAPIDocsInterpreter
import sttp.tapir.openapi.circe.yaml._
import sttp.tapir.swagger.akkahttp.SwaggerAkka


object Server {

    sealed trait Message
    private final case class StartFailed(cause: Throwable) extends Message
    private final case class Started(binding: ServerBinding) extends Message
    case object Stop extends Message

    def apply(host: String, port: Int): Behavior[Message] = Behaviors.setup { ctx =>

        implicit val system = ctx.system
        val sessionsStore = ctx.spawn(SessionStore(), "sessions")
        ctx.watch(sessionsStore)

        val sessionsRoutes = new SessionHandlers(sessionsStore);


        val myEndpoints: Seq[Endpoint[_, _, _, _]] = Seq(SessionEndpoints.createSession.endpoint, SessionEndpoints.addStory.endpoint, SessionEndpoints.createGame.endpoint, SessionEndpoints.moveCard.endpoint, SessionEndpoints.nextPlayer.endpoint, SessionEndpoints.getGame.endpoint, SessionEndpoints.getSession.endpoint)
        val docsAsYaml: String = OpenAPIDocsInterpreter().toOpenAPI(myEndpoints, "My App", "1.0").toYaml
        val swagger = new SwaggerAkka(docsAsYaml)


        val serverBinding: Future[Http.ServerBinding] = Http().newServerAt(host, port).bind(sessionsRoutes.routes ~ swagger.routes)
        ctx.pipeToSelf(serverBinding) {
            case Success(binding) => Started(binding)
            case Failure(ex)      => StartFailed(ex)
        }

        def running(binding: ServerBinding): Behavior[Message] =
            Behaviors.receiveMessagePartial[Message] {
                case Stop =>
                    ctx.log.info(
                        "Stopping server http://{}:{}/",
                        binding.localAddress.getHostString,
                        binding.localAddress.getPort)
                    Behaviors.stopped
            }.receiveSignal {
                case (_, PostStop) =>
                    binding.unbind()
                    Behaviors.same
            }

        def starting(wasStopped: Boolean): Behaviors.Receive[Message] =
            Behaviors.receiveMessage[Message] {
                case StartFailed(cause) =>
                    throw new RuntimeException("Server failed to start", cause)
                case Started(binding) =>
                    ctx.log.info(
                        "Server online at http://{}:{}/",
                        binding.localAddress.getHostString,
                        binding.localAddress.getPort)
                    if (wasStopped) ctx.self ! Stop
                    running(binding)
                case Stop =>
                    // we got a stop message but haven't completed starting yet,
                    // we cannot stop until starting has completed
                    starting(wasStopped = true)
            }

        starting(wasStopped = false)
    }
}


object Main extends App {
    val system: ActorSystem[Server.Message] =
        ActorSystem(Server("localhost", 8081), "BuildJobsServer")
}
