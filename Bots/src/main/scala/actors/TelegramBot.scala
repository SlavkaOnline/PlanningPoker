package planning.poker
package actors

import akka.NotUsed
import akka.actor.typed.scaladsl.Behaviors
import akka.actor.typed.scaladsl.LoggerOps
import akka.actor.typed.{ActorRef, ActorSystem, Behavior, Terminated}

object TelegramBot {

    final case class User(id: String, name: String)

    sealed trait Command
    final case class ConnectToSession(user: User, session: String) extends Command
    private final case class CreateSession(userToken: String, session: String) extends Command

    def apply():Behavior[Command] =
        telegramBot(Map.empty)

    def telegramBot(sessions: Map[User, Map[String,ActorRef[Session.Command]]]): Behavior[Command] =
        Behaviors.setup { context =>

            Behaviors.receiveMessage {
                case ConnectToSession(user, session) =>
                    if (sessions.contains(user)) {
                        if (!sessions(user).contains(session))
                                context.spawn(Session(), session)
                        Behaviors.same
                    } else {

                    }
                    Behaviors.same
            }
    }
}
