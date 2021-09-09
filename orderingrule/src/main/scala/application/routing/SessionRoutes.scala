package application.routing

import actors._
import akka.actor.typed.{ActorRef, ActorSystem, Scheduler}
import akka.http.scaladsl.model.StatusCode
import akka.http.scaladsl.server.Directives.{_regex2PathMatcher, as, complete, concat, decodeRequest, entity, onSuccess, path, pathPrefix, post}
import akka.http.scaladsl.server.Route
import akka.util.Timeout
import application.{Requests, Views}
import akka.actor.typed.scaladsl.AskPattern.schedulerFromActorSystem

import java.util.UUID
import scala.concurrent.Future
import scala.concurrent.duration.DurationInt
import application.JacksonSupport._
import application.Views.{PlayerView, SessionView}
import domain.Session


class SessionRoutes(sessionsStore: ActorRef[SessionStore.Message])(implicit system: ActorSystem[_]) {

    import akka.actor.typed.scaladsl.AskPattern.schedulerFromActorSystem
    import akka.actor.typed.scaladsl.AskPattern.Askable

    implicit val timeout: Timeout = 5.seconds

    val uuidRegex = """[\da-fA-F]{8}-[\da-fA-F]{4}-[\da-fA-F]{4}-[\da-fA-F]{4}-[\da-fA-F]{12}"""
    val owner: UUID = UUID.randomUUID()
    lazy val routes: Route =
        path("sessions") {
            concat(
                post {
                    decodeRequest {
                        entity(as[Requests.CreateSession]) { request =>

                            val f = sessionsStore.ask(SessionStore.AddSession(UUID.randomUUID(), owner, request.name, _))
                            onSuccess(f) { r => complete((StatusCode.int2StatusCode(201), r)) }
                        }
                    }
                    }
//                },
//                post {
//                    path(s"($uuidRegex)".r / "story") { id =>
//                        decodeRequest {
//                            entity(as[Requests.AddStory]) { request =>
//                                val f = for {
//                                    sessionActor <- sessionsStore.ask(SessionStore.GetSession(UUID.fromString(id), _))(timeout, scheduler)
//                                    session <- sessionActor.ask(SessionActor.AddStory(owner, request.name, _))(timeout, system.scheduler)
//                                    view = sessionViewMap(session)
//                                } yield view
//                                onSuccess(f) { r => complete((StatusCode.int2StatusCode(200), r)) }
//                            }
//                        }
//                    }
//                }
            )
        }
}
