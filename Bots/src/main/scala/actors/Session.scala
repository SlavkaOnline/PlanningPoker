package planning.poker
package actors

import akka.actor.typed.scaladsl.Behaviors
import akka.actor.typed.{ActorRef, ActorSystem, Behavior, Terminated}

object Session {

    sealed trait Command

    def apply(): Behavior[Command] =
        session()

    def session(): Behavior[Command] = Behaviors.same
}
