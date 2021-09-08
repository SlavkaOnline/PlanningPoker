package application

import akka.http.scaladsl.marshallers.sprayjson.SprayJsonSupport
import spray.json._

object Requests  {
    final case class CreateSession(name: String)
}
