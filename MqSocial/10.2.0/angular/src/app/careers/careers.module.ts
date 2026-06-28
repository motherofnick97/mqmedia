import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { CareersRoutingModule } from './careers-routing.module';

@NgModule({
    imports: [
        SharedModule,
        CareersRoutingModule,
    ],
})
export class CareersModule {}
